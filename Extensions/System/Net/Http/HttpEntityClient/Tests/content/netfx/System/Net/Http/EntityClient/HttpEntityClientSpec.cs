using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.Linq.Expressions;
using System.Net.Http;
using Microsoft.ApplicationServer.Http.Description;
using System.Net;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.ApplicationServer.Http.Dispatcher;
using System.Web;
using Microsoft.ApplicationServer.Http;
using Microsoft.ApplicationServer.Http.Activation;

namespace Tests
{
	public class HttpEntityClientSpec
	{
		private const string resourceName = "products";

		[Fact]
		public void WhenGetting_ThenRetrieves()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);

				var product = client.Get<Product>(resourceName, "1");

				Assert.NotNull(product);
				Assert.Equal("kzu", product.Owner.Name);
			}
		}

		[Fact]
		public void WhenGettingWithoutId_ThenRetrieves()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);

				var product = client.Get<Product>(resourceName + "/latest");

				Assert.NotNull(product);
				Assert.Equal("vga", product.Owner.Name);
			}
		}

		[Fact]
		public void WhenPostNew_ThenSavesAndRetrievesId()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var product = new Product { Owner = new User { Id = 1, Name = "kzu" } };

				var saved = client.Post(resourceName, product);

				Assert.Equal(4, saved.Id);

				Assert.Equal(saved.Owner.Id, product.Owner.Id);
				Assert.Equal(saved.Owner.Name, product.Owner.Name);
			}
		}

		[Fact]
		public void WhenDeletingEntity_ThenGetFails()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);

				client.Delete(resourceName, "1");
				var exception = Assert.Throws<HttpEntityException>(() => client.Get<Product>(resourceName, "25"));

				Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
			}
		}

		[Fact]
		public void WhenDeleteFails_ThenThrows()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);

				var exception = Assert.Throws<HttpEntityException>(() => client.Delete(resourceName, "25"));

				Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
			}
		}

		[Fact]
		public void WhenPostFails_ThenThrows()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);

				var exception = Assert.Throws<HttpEntityException>(() => client.Post<Product>(resourceName, null));

				Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
			}
		}

		[Fact]
		public void WhenPutNew_ThenSaves()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", true, new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var product = new Product { Owner = new User { Id = 1, Name = "kzu" } };

				client.Put(resourceName, "4", product);

				var saved = client.Get<Product>(resourceName, "4");

				Assert.Equal(saved.Id, 4);
				Assert.Equal(saved.Owner.Id, product.Owner.Id);
				Assert.Equal(saved.Owner.Name, product.Owner.Name);
			}
		}

		[Fact]
		public void WhenPutFails_ThenThrows()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				// We're putting a null which is invalid.
				var exception = Assert.Throws<HttpEntityException>(() => client.Put<Product>(resourceName, "25", null));

				Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
			}
		}

		[Fact]
		public void WhenPutUpdate_ThenSaves()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", true, new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var product = new Product { Id = 1, Owner = new User { Id = 1, Name = "vga" } };

				client.Put(resourceName, "1", product);

				var saved = client.Get<Product>(resourceName, "1");

				Assert.Equal(saved.Owner.Name, product.Owner.Name);
			}
		}

		[Fact]
		public void WhenGetFails_ThenThrows()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);

				var exception = Assert.Throws<HttpEntityException>(() => client.Get<Product>(resourceName, "25"));

				Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
			}
		}

		[Fact]
		public void WhenTryGetFails_ThenReturnsResponse()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var product = default(Product);
				var response = client.TryGet<Product>(resourceName, "25", out product);

				Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
				Assert.Null(product);
			}
		}

		[Fact]
		public void WhenTryGetSucceeds_ThenPopulatesEntity()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var product = default(Product);
				var response = client.TryGet<Product>(resourceName, "1", out product);

				Assert.True(response.IsSuccessStatusCode);
				Assert.NotNull(product);
				Assert.Equal("kzu", product.Owner.Name);
			}
		}

		[Fact]
		public void WhenQuerying_ThenPopulatesMatchingEntities()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var ids = client.Query<Product>(resourceName).Where(x => x.Owner.Name == "kzu").Select(x => x.Id).ToList();
				var products = client.Query<Product>(resourceName).Where(x => x.Owner.Name == "kzu").ToList();

				Assert.Equal(2, ids.Count);
				Assert.True(products.All(x => x.Owner.Name == "kzu"));
			}
		}

		[Fact]
		public void WhenQueryingAndNoMatches_ThenReturnsEmptyEnumerable()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var products = client.Query<Product>(resourceName).Where(x => x.Owner.Name == "foo").ToList();

				Assert.Equal(0, products.Count);
			}
		}

		[Fact]
		public void WhenSkipTakeOnly_ThenReturnsSingleElement()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var products = client.Query<Product>(resourceName).Skip(1).Take(1).ToList();

				Assert.Equal(1, products.Count);
				Assert.Equal(2, products[0].Id);
			}
		}

		[Fact]
		public void WhenOrderByTake_ThenReturnsOrdered()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var products = client.Query<Product>(resourceName).OrderBy(x => x.Title).Take(2).ToList();

				Assert.Equal(2, products.Count);
				Assert.Equal("A", products[0].Title);
				Assert.Equal("B", products[1].Title);
			}
		}

		[Fact]
		public void WhenQueryingWithExtraCriteria_ThenPopulatesMatchingEntities()
		{
			using (var ws = new HttpWebService<TestService>("http://localhost:20000", "products", new ServiceConfiguration()))
			{
				var client = new HttpEntityClient(ws.BaseUri);
				var products = client.Query<Product>(resourceName, new { search = "kzu" }).ToList();

				Assert.True(products.All(x => x.Owner.Name == "kzu"));
			}
		}

		[Fact]
		public void WhenQuerying_ThenCanGetTotalCount()
		{
			var baseUri = new Uri("http://localhost:20000");
			var service = new TestService();
			var config = new ServiceConfiguration(service);

			using (new SafeHostDisposer(
				new HttpQueryableServiceHost(typeof(TestService), 25, config, new Uri(baseUri, "products"))))
			{
				var client = new HttpEntityClient(baseUri);
				var products = client.Query<Product>(resourceName, new { search = "kzu" }).Skip(5).Take(10);

				var query = products as IHttpEntityQuery<Product>;
				var response = query.Execute();

				Assert.Equal(2, response.TotalCount);
				Assert.Equal(0, response.Count());
				Assert.True(response.Response.IsSuccessStatusCode);
			}
		}

		public class ServiceConfiguration : HttpHostConfiguration
		{
			public ServiceConfiguration(object serviceInstance = null)
			{
				this.OperationHandlerFactory.Formatters.Insert(0, new JsonNetMediaTypeFormatter());
				this.AddMessageHandlers(typeof(TracingChannel));
				this.SetErrorHandler<ErrorHandler>();
				if (serviceInstance != null)
					this.Configure.SetResourceFactory(new SingletonResourceFactory(serviceInstance));
			}
		}

		public class ErrorHandler : Microsoft.ApplicationServer.Http.Dispatcher.HttpErrorHandler
		{
			protected override bool OnHandleError(Exception error)
			{
				System.Diagnostics.Trace.TraceError(error.ToString());
				return true;
			}

			protected override HttpResponseMessage OnProvideResponse(Exception error)
			{
				return new HttpResponseMessage(HttpStatusCode.InternalServerError, error.Message);
			}
		}
	}
}
