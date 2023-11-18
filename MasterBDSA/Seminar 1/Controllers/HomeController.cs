using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Seminar_1.Models.Entities;
using Seminar_1.Models.VMs;
using Microsoft.Extensions.Hosting;
using PayPal.Api;
using System;
using System.Collections.Generic;

namespace Seminar_1.Controllers
{
    public class HomeController : Controller
    {
        private readonly Seminar1Context context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private IConfiguration configuration;
        private Payment payment;

        public HomeController(Seminar1Context context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            this.context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
        }

        public IActionResult Index()
        {
            var list = context.Products.Select(p => new ProductVM().ProdToProdVM(p)).ToList();
            return View(list);
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            var list = context.Products.Select(p => new ProductVM().ProdToProdVM(p)).ToList();
            var product = list.FirstOrDefault(p => p.Id == id);
            return View(product);
        }

        [HttpPost]
        [Route("Add/{id}")]
        public IActionResult Add(int id)
        {
            var shopList = HttpContext.Session.Get<List<int>>(SessionHelper.ShoppingCart);

            if (shopList == null)
                shopList = new List<int>();

            if (!shopList.Contains(id))
                shopList.Add(id);

            HttpContext.Session.Set(SessionHelper.ShoppingCart, shopList);
            var list = context.Products.Select(p => new ProductVM().ProdToProdVM(p)).ToList();

            return RedirectToAction("Index", "Home", list);
        }

        [HttpPost]
        [Route("Remove/{id}")]
        public IActionResult Remove(int id)
        {
            var shopList = HttpContext.Session.Get<List<int>>(SessionHelper.ShoppingCart);
            var list = context.Products.Select(p => new ProductVM().ProdToProdVM(p)).ToList();

            if (shopList == null)
                return RedirectToAction("Index", "Home", list);

            if (shopList.Contains(id))
                shopList.Remove(id);

            HttpContext.Session.Set(SessionHelper.ShoppingCart, shopList);

            return RedirectToAction("Index", "Home", list);
        }

        public ActionResult PaymentWithPayPal(string cancel = null, string blogId = "", string payerId = "", string guid = "")
        {
            var clientId = configuration.GetValue<string>("PayPal:Key");
            var clientSecret = configuration.GetValue<string>("PayPal:Secret");
            var mode = configuration.GetValue<string>("PayPal:mode");
            var apiContext = PayPalConfiguration.GetAPIContext(clientId, clientSecret, mode);

            try
            {
                if (string.IsNullOrWhiteSpace(payerId))
                {
                    var baseURI = this.Request.Scheme + "://" + this.Request.Host + "/Home/PaymentWithPayPal?";
                    var guidd = Convert.ToString((new Random()).Next(100000));
                    guid = guidd;

                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid, blogId);
                    var links = createdPayment.links.GetEnumerator();
                    string? paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        var lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            paypalRedirectUrl = lnk.href;
                        }
                    }

                    httpContextAccessor.HttpContext.Session.SetString("payment", createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    var paymentId = httpContextAccessor.HttpContext.Session.GetString("payment");
                    var executedPayment = ExecutePayment(apiContext, payerId, paymentId);
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("PaymentFailed");
                    }

                    var blogs = executedPayment.transactions[0].item_list.items[0].sku;
                    var shopList = HttpContext.Session.Get<List<int>>(SessionHelper.ShoppingCart);
                    shopList.Clear();
                    HttpContext.Session.Set(SessionHelper.ShoppingCart, shopList);
                    return View("PaymentSuccess");
                }
            }
            catch (Exception e)
            {
                return View("PaymentFailed");
            }
        }

        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId,
            };

            this.payment = new Payment()
            {
                id = paymentId,
            };

            return this.payment.Execute(apiContext, paymentExecution);
        }

        private Payment CreatePayment(APIContext apiContext, string redirectUrl, string blogId)
        {
            var shopList = HttpContext.Session.Get<List<int>>(SessionHelper.ShoppingCart);
            var productsList = context.Products.Select(p => new ProductVM().ProdToProdVM(p)).ToList();
            if (shopList == null)
                View("Shopping cart empty");

            var products = productsList.Where(p => shopList.Any(s => s == p.Id)).ToList();

            var itemList = new ItemList()
            {
                items = new List<Item>()
            };

            var items = products.Select(p => new Item()
            {
                name = p.Name,
                currency = "USD",
                price = p.Price.ToString(),
                quantity = "1",
                sku = "asd"
            }).ToList();

            itemList.items.AddRange(items);

            var payer = new Payer()
            {
                payment_method = "paypal"
            };

            var redirectUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true",
                return_url = redirectUrl
            };

            var amount = new Amount()
            {
                currency = "USD",
                total = products.Sum(p => p.Price).ToString()
            };

            var tramsactionList = new List<Transaction>();

            tramsactionList.Add(new Transaction()
            {
                description = "Transaction description",
                invoice_number = Guid.NewGuid().ToString(),
                amount = amount,
                item_list = itemList
            });

            this.payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = tramsactionList,
                redirect_urls = redirectUrls
            };

            return this.payment.Create(apiContext);
        }
    }
}