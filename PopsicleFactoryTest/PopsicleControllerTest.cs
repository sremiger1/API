using API.Controllers;
using API.Database;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Runtime.CompilerServices;

namespace PopsicleFactoryTest
{
    public class Tests
    {

        private IPopsicleService _service;
        private List<DBPopsicle>  _data;

        private void SetupData()
        {
            _data = new List<DBPopsicle>
            {
                new DBPopsicle(){ Color = "Green", CreateDate = DateTime.Now, Description = "Green Description",  Ingredients="Green Ingredients",  LastUpdate= DateTime.Now, Name = "Green Name",  Quantity = 100,  SKU= "GREENSKU", Id = 1  },
                new DBPopsicle(){ Color = "Blue", CreateDate = DateTime.Now, Description = "Blue Description",  Ingredients="Blue Ingredients",  LastUpdate= DateTime.Now, Name = "Blue Name",  Quantity = 10,  SKU= "BLUESKU", Id = 2  },
                new DBPopsicle(){ Color = "Red", CreateDate = DateTime.Now, Description = "Red Description",  Ingredients="Red Ingredients",  LastUpdate= DateTime.Now, Name = "Red Name",  Quantity = 3,  SKU= "REDSKU", Id = 3  },
            };
        }

        private Mock<DbSet<DBPopsicle>> SetupMockDBSet()
        {
            var qry = _data.AsQueryable();
            var mockSet = new Mock<DbSet<DBPopsicle>>();
            mockSet.As<IQueryable<DBPopsicle>>().Setup(m => m.Provider).Returns(qry.Provider);
            mockSet.As<IQueryable<DBPopsicle>>().Setup(m => m.Expression).Returns(qry.Expression);
            mockSet.As<IQueryable<DBPopsicle>>().Setup(m => m.ElementType).Returns(qry.ElementType);
            mockSet.As<IQueryable<DBPopsicle>>().Setup(m => m.GetEnumerator()).Returns(() => qry.GetEnumerator());
            mockSet.As<IDbAsyncEnumerable<DBPopsicle>>().Setup(x => x.GetAsyncEnumerator()).Returns(new TestDbAsyncEnumerator<DBPopsicle>(qry.GetEnumerator()));

            //mockSet.As<IQueryable<DBPopsicle>>().Setup(m => m.GetEnumerator()).Returns(() => qry.GetEnumerator());
            mockSet.Setup(d => d.Add(It.IsAny<DBPopsicle>())).Callback<DBPopsicle>(
                (s) => {
                    s.Id = _data.Count + 1;
                    _data.Add(s);
                });
            mockSet.Setup(d => d.Remove(It.IsAny<DBPopsicle>())).Callback<DBPopsicle>(
                (s) => {
                    _data.Remove(s);
                });

            return mockSet;
        }

        [SetUp]
        public void Setup()
        {
            SetupData();
            var mockSet = SetupMockDBSet();
            var mockContext = new Mock<PopsicleContext>();
            mockContext.Setup(c => c.Popsicle).Returns(mockSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);
            mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var mockDbFactory = new Mock<Microsoft.EntityFrameworkCore.IDbContextFactory<PopsicleContext>>();
            mockDbFactory.Setup(f => f.CreateDbContext()).Returns(mockContext.Object);
            mockDbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockContext.Object);

            _service = new PopsicleService(mockDbFactory.Object);
        }

        

        [Test(Description = "Scenario: Promise Broken - Popsicle Request is Invalid")] 
        public async Task PopsicleRequestisInvalid() 
        {
            var contoller = new PopsicleController(_service);
            var response = await contoller.Get(-1) as BadRequestObjectResult;
            Assert.NotNull(response);
            Assert.AreEqual(400, response.StatusCode);
        }
        [Test(Description = "Scenario: Promise Broken - Popsicle does not exist")]
        public async Task PopsicleDoesNotExist()
        {
            var id = 100;
            var contoller = new PopsicleController(_service);
            var response = await contoller.Get(id) as NotFoundObjectResult;
            Assert.NotNull(response);
            Assert.AreEqual(404, response.StatusCode);
            Assert.AreEqual($"Popsicle with Id: {id} not found", response.Value as string);
        }


        [Test(Description = "Scenario: Create Popsicle")]
        public async Task CreatePopsicle()
        {
            var value = new Popsicle(0, "YELLOWSKU", 10, "Yellow Name", "yellow Description", "Yellow Color", "Yellow Ingredients");
            var contoller = new PopsicleController(_service);
            var response = await contoller.Create(value) as OkObjectResult;
            Assert.NotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            var output = response.Value as Popsicle;
            Assert.NotNull(output);
            Assert.AreEqual("YELLOWSKU", output.SKU);
        }


        [Test(Description = "Scenario: Replace Popsicle")]
        public async Task ReplacePopsicle()
        {           
            var value = new Popsicle(1, "SKUGREEN", 5, "Name Green", "Description Green", "Color Green", "Ingredients Green" );
            var contoller = new PopsicleController(_service);
            var response = await contoller.Replace(value.Id, value) as OkObjectResult;
            Assert.NotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            var output = response.Value as Popsicle;
            Assert.NotNull(output);
            Assert.AreEqual("SKUGREEN", output.SKU);


            value = new Popsicle(1, null, 15, "Name Green", "Description Green", "Color Green", "Ingredients Green");
            contoller = new PopsicleController(_service);
            var badRequestResponse = await contoller.Replace(value.Id, value) as BadRequestObjectResult;
            Assert.NotNull(badRequestResponse);
            Assert.AreEqual(400, badRequestResponse.StatusCode);
            var outputMS = badRequestResponse.Value as Dictionary<string, string[]>;
            Assert.NotNull(outputMS);
            Assert.AreEqual(1, outputMS.Count());
            Assert.AreEqual("Popsicle is missing SKU", outputMS.First().Value[0]);


            value = new Popsicle(0, "SKUORANGE", 15, "Name Orange", "Description Orange", "Color Orange", "Ingredients Orange");
            contoller = new PopsicleController(_service);
            Mock<IUrlHelper> urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(_ => _.Link(It.IsAny<string>(), It.IsAny<object>())).Returns((string name, object values) =>
            {
                return $"http://localhost/api/popiscle/4";
            });
            contoller.Url = urlHelper.Object;
            var createResponse = await contoller.Replace(null, value) as CreatedResult;
            Assert.NotNull(createResponse);
            Assert.AreEqual(201, createResponse.StatusCode);
            output = createResponse.Value as Popsicle;
            Assert.NotNull(output);
            Assert.AreEqual("SKUORANGE", output.SKU);

        }


        [Test(Description = "Scenario: Update Popsicle")]
        public async Task UpdatePopsicle()
        {
            var value = _data[0];
            
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Replace("SKU", "PATCHSKU");
            patchDoc.Replace("Description", "PATCH Description");
            var contoller = new PopsicleController(_service);
            var response = await contoller.Patch(value.Id,  patchDoc) as OkObjectResult;
            Assert.NotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            var output = response.Value as Popsicle;
            Assert.NotNull(output);
            Assert.AreEqual("PATCHSKU", output.SKU);
            Assert.AreEqual("PATCH Description", output.Description);
            Assert.AreEqual(value.Color, output.Color);
            Assert.AreEqual(value.Ingredients, output.Ingredients);

            var id = 100;
            patchDoc = new JsonPatchDocument();
            patchDoc.Replace("SKU", "PATCHSKU1");
            patchDoc.Replace("Description", "PATCH Description1");
            contoller = new PopsicleController(_service);
            var notFoundResponse = await contoller.Patch(id, patchDoc) as NotFoundObjectResult;
            Assert.NotNull(notFoundResponse);
            Assert.AreEqual(404, notFoundResponse.StatusCode);
            Assert.AreEqual($"Popsicle with Id: {id} not found", notFoundResponse.Value as string);

        }

        [Test(Description = "Scenario: Remove Popsicle")]
        public async Task RemovePopsicle()
        {
            var contoller = new PopsicleController(_service);
            var response = await contoller.Delete(1) as OkObjectResult;
            Assert.NotNull(response);
            Assert.AreEqual(200, response.StatusCode);

            contoller = new PopsicleController(_service);
            response = await contoller.Delete(-1) as OkObjectResult;
            Assert.NotNull(response);
            Assert.AreEqual(200, response.StatusCode);
        }


        [Test(Description = "Scenario: Get Popsicle")]
        public async Task GetPopsicle()
        {
            var contoller = new PopsicleController(_service);
            var response = await contoller.Get(1) as OkObjectResult;
            Assert.NotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            var output = response.Value as Popsicle;
            Assert.NotNull(output);
            Assert.AreEqual("GREENSKU", output.SKU);

        }

        [Test(Description = "Scenario: Search Popsicles")]
        public async Task SearchPopsicles()
        {
            var contoller = new PopsicleController(_service);
            var response = await contoller.Search("SKU") as OkObjectResult;
            Assert.NotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            var output = response.Value as IList<Popsicle>;
            Assert.NotNull(output);
            Assert.IsNotEmpty(output);

            contoller = new PopsicleController(_service);
            var noContentResponse = await contoller.Search("KORTERRA") as NoContentResult;
            Assert.NotNull(noContentResponse);
            Assert.AreEqual(204, noContentResponse.StatusCode);
        }


        
    }
}