﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Net.Http;
using Microsoft.ApplicationServer.Http;

[ServiceContract]
public class TestService
{
	// TODO: this could come from the same pluralizer/formatter as the client.
	private const string resourceName = "products";
	private List<Product> products;

	public TestService()
	{
		this.products = new List<Product>
		{
			new Product
			{
				Id = 1,
				Owner = new User
				{
					Id = 1,
					Name = "kzu",
				}
			}, 
			new Product
			{
				Id = 2,
				Owner = new User
				{
					Id = 1,
					Name = "kzu",
				}
			}, 
			new Product
			{
				Id = 3,
				Owner = new User
				{
					Id = 2,
					Name = "vga",
				}
			}, 
		};
	}

	[WebGet(UriTemplate = "{id}")]
	public HttpResponseMessage Get(int id)
	{
		var product = this.products.FirstOrDefault(x => x.Id == id);

		if (product == null)
			return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound, "Not found");

		return new HttpResponseMessage<Product>(product);
	}

	[WebInvoke(Method = "DELETE", UriTemplate = "{id}")]
	public HttpResponseMessage Delete(int id)
	{
		var product = this.products.FirstOrDefault(x => x.Id == id);

		if (product == null)
			return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound, "Not found");

		this.products.Remove(product);

		return new HttpResponseMessage(System.Net.HttpStatusCode.OK, "Deleted");
	}

	[WebInvoke(Method = "POST", UriTemplate = "")]
	public HttpResponseMessage Create(Product product)
	{
		var id = this.products.Select(x => x.Id).OrderBy(x => x).Last() + 1;
		product.Id = id;

		this.products.Add(product);

		var response = new HttpResponseMessage<Product>(product, System.Net.HttpStatusCode.Created);
		response.Headers.Location = new Uri(resourceName + "/" + id.ToString(), UriKind.Relative);

		return response;
	}

	//[WebInvoke(Method = "POST", UriTemplate = "{id}")]
	//public HttpResponseMessage Update(int id, Product product)
	//{
	//    var existing = this.products.FirstOrDefault(x => x.Id == id);

	//    if (existing == null)
	//        return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound, "Not found");

	//    product.Id = id;
	//    this.products.Remove(existing);
	//    this.products.Add(product);

	//    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted, "Updated");
	//}

	[WebInvoke(Method = "PUT", UriTemplate = "{id}")]
	public HttpResponseMessage CreateOrUpdate(int id, Product product)
	{
		var existing = this.products.FirstOrDefault(x => x.Id == id);
		product.Id = id;

		if (product == null)
		{
			this.products.Add(product);
			return new HttpResponseMessage(System.Net.HttpStatusCode.Created, "Created");
		}
		else
		{
			this.products.Remove(existing);
			this.products.Add(product);
			return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted, "Updated");
		}
	}

	[WebGet(UriTemplate = "")]
	public IQueryable<Product> All()
	{
		return this.products.AsQueryable();
	}
}

public class Product
{
	public int Id { get; set; }
	public User Owner { get; set; }
}

public class User
{
	public int Id { get; set; }
	public string Name { get; set; }
}
