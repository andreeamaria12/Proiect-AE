﻿IF (DB_ID(N'ProiectAE') IS NULL)
	CREATE DATABASE ProiectAE
GO

USE ProiectAE
GO

IF OBJECT_ID ('Products') IS NULL
	CREATE TABLE Products
	(
	Id INT NOT NULL IDENTITY(1, 1) CONSTRAINT PK_Product PRIMARY KEY,
	[Name] NVARCHAR(256) NOT NULL,
	[Description] NVARCHAR(2000) NOT NULL, 
	Price NUMERIC(20, 2) NOT NULL,
	IsAvailable BIT NOT NULL,
	ImagePath NVARCHAR(1000) NULL
	)
GO