using API.Database;
using API.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API.Services
{
    public interface IPopsicleService
    {
        /// <summary>
        /// Searchs  SKU, Ingredients, Name, Description, Color
        /// </summary>
        /// <param name="searchValue"></param>
        /// <returns></returns>
        Task<IList<Popsicle>> Search(string searchValue);

        /// <summary>
        /// Gets a Popsicle
        /// </summary>
        /// <param name="id"></param>
        /// <returns>If not found returns null</returns>
        Task<Popsicle> Get(int id);

        /// <summary>
        /// Saves a Popsicle to the DB
        /// </summary>
        /// <param name="prd">Popsicle to save</param>
        /// <returns>Returns committed popsicle</returns>
        Task<Popsicle> Create(Popsicle prd);

        /// <summary>
        /// Updates or Creates new popsicle
        /// </summary>
        /// <param name="id">Id to update if not found will create</param>
        /// <param name="prd">Data to persist</param>
        /// <returns>True if created false if update, and the popiscle from the database</returns>
        Task<(bool, Popsicle)> CreateOrUpdate(int? id, Popsicle prd);

        /// <summary>
        /// Update specific fields of the popiscle
        /// </summary>
        /// <param name="id">Key to look up</param>
        /// <param name="prd">Path information</param>
        /// <returns>Updated popsicle from DB</returns>
        Task<Popsicle> Patch(int id, JsonPatchDocument prd);

        /// <summary>
        /// Validates that the popsicle can be updated
        /// </summary>
        /// <param name="id">Id to validate against</param>
        /// <param name="prd">Popsicle</param>
        /// <param name="modelState">Dictionary of errors returned if found</param>
        /// <returns>False if invalid true if valid</returns>
        Task<bool> Validate(int? id, Popsicle prd, ModelStateDictionary modelState);

        /// <summary>
        /// Removes record from DB
        /// </summary>
        /// <param name="id"></param>
        Task Delete(int id);

        void Seed();
    }

    public class PopsicleService : IPopsicleService
    {
        private readonly IDbContextFactory<PopsicleContext> _dbContext;

        public PopsicleService(IDbContextFactory<PopsicleContext> dbContext)
        {
            _dbContext = dbContext;
        }
        private Task<PopsicleContext> GetContextAsync()
        {
            return _dbContext.CreateDbContextAsync();
        }
        public async Task Delete(int Id) {
            var db = await GetContextAsync();
            var dbPrd = db.Popsicle.FirstOrDefault(_ => _.Id == Id);
            db.Popsicle.Remove(dbPrd);
            await db.SaveChangesAsync();
        }

        public async Task<Popsicle> Get(int Id)
        {
            var db = await GetContextAsync();
            var dbPop = db.Popsicle.FirstOrDefault(_ => _.Id == Id);
            if (dbPop == null) return null;
            return new Popsicle(dbPop.Id, dbPop.SKU, dbPop.Quantity, dbPop.Name, dbPop.Description, dbPop.Color, dbPop.Ingredients);
        }
        public async Task<bool> Validate(int? id, Popsicle prd, ModelStateDictionary ms)
        {

            if( prd == null) { 
                ms.AddModelError("Popsicle", "Popsicle is missing");
                return ms.IsValid;
            }            
            
            if (string.IsNullOrEmpty(prd.SKU)) { ms.AddModelError("SKU", "Popsicle is missing SKU"); }
            if (string.IsNullOrEmpty(prd.Name)) { ms.AddModelError("Name", "Popsicle is missing Name"); }
            if (string.IsNullOrEmpty(prd.Description)) { ms.AddModelError("Description", "Popsicle is missing Description"); }
            if (string.IsNullOrEmpty(prd.Color)) { ms.AddModelError("Color", "Popsicle is missing Color");  }
            if (string.IsNullOrEmpty(prd.Ingredients)) { ms.AddModelError("Ingredients", "Popsicle is missing Ingredients");  }

            var db = await GetContextAsync();
            
            if (id.HasValue)
            {
                if(db.Popsicle.Any(_ => _.Id != id.Value && _.SKU == prd.SKU))
                {
                    ms.AddModelError("SKU", $"Popsicle SKU {prd.SKU} already exists.");
                }
            }
            else
            {
                //New Products must have a qty existing can be set to 0
                if (prd.Quantity == 0) { ms.AddModelError("Quantity", "Popsicle is missing Quantity"); }

                if (db.Popsicle.Any(_ => _.SKU == prd.SKU))
                {
                    ms.AddModelError("SKU", $"Popsicle SKU {prd.SKU} already exists.");
                }
            }


            return ms.IsValid;
        }

        public async Task<Popsicle> Create(Popsicle prd)
        {
            var db = await GetContextAsync();
            var dbPop = new DBPopsicle()
            {
                CreateDate = DateTime.Now,
                SKU = prd.SKU,
                Quantity = prd.Quantity,
                LastUpdate = DateTime.UtcNow,
                Name = prd.Name,
                Description = prd.Description,
                Color = prd.Color,
                Ingredients = prd.Ingredients
            };
            db.Popsicle.Add(dbPop);
            await db.SaveChangesAsync();
            return new Popsicle(dbPop.Id, dbPop.SKU, dbPop.Quantity, dbPop.Name, dbPop.Description, dbPop.Color, dbPop.Ingredients);
        }

        public async Task<IList<Popsicle>> Search(string searchValue)
        {
            var db = await GetContextAsync();
            return db.Popsicle.Where(_ => _.SKU.Contains(searchValue)
                                                    || _.Ingredients.Contains(searchValue)
                                                    || _.Name.Contains(searchValue)
                                                    || _.Description.Contains(searchValue)
                                                    || _.Color.Contains(searchValue))
                    .Select(_ => new Popsicle(_.Id, _.SKU, _.Quantity, _.Name, _.Description, _.Color, _.Ingredients))
                    .ToList();        
        }

        public async Task<(bool, Popsicle)> CreateOrUpdate(int? id, Popsicle prd)
        {
            if (!id.HasValue)
            {
                var p = await Create(prd);
                return (true, p);
            }
            var db = await GetContextAsync();
            var dbPop = db.Popsicle.FirstOrDefault(_ => _.Id == id.Value);
            if(dbPop == null)
            {
                var p = await Create(prd);
                return (true, p);
            }

            dbPop.SKU = prd.SKU;
            dbPop.Quantity = prd.Quantity;
            dbPop.LastUpdate = DateTime.UtcNow;
            dbPop.Name = prd.Name;
            dbPop.Description = prd.Description;
            dbPop.Color = prd.Color;
            dbPop.Ingredients = prd.Ingredients;
            await db.SaveChangesAsync();
            return (false, new Popsicle(dbPop.Id, dbPop.SKU, dbPop.Quantity, dbPop.Name, dbPop.Description, dbPop.Color, dbPop.Ingredients));
        }

        public async Task<Popsicle> Patch(int Id,JsonPatchDocument prd)
        {
            var db = await GetContextAsync();
            var dbPrd = db.Popsicle.FirstOrDefault(p => p.Id == Id);
            if (dbPrd == null)
            {
                return null;
            }
            prd.ApplyTo(dbPrd);
            await db.SaveChangesAsync();
            return await Get(Id);
        }

        public void Seed()
        {
            var db = _dbContext.CreateDbContext();
            db.Popsicle.Add(new DBPopsicle()
            {
                CreateDate = DateTime.Now,
                SKU = "SKU1",
                Quantity = 10,
                LastUpdate = DateTime.UtcNow,
                Name = "Name1",
                Description = "Descrption1",
                Color = "Color1",
                Ingredients = "Ingredients1"
            });

            db.Popsicle.Add(new DBPopsicle()
            {
                CreateDate = DateTime.Now,
                SKU = "SKU2",
                Quantity = 10,
                LastUpdate = DateTime.UtcNow,
                Name = "Name2",
                Description = "Descrption2",
                Color = "Color2",
                Ingredients = "Ingredients2"
            });

            db.SaveChanges();
        }
    }
}
