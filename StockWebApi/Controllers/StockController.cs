using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using StockWebApi.Models;

namespace StockWebApi.Controllers
{
    public class StockController : ApiController
    {
        public IHttpActionResult Get(long productID)
        {
            IList<Stockist> listOfStockists=null;
            using (StockDBEntities1 db = new StockDBEntities1())
            {
                db.Configuration.ProxyCreationEnabled = false;
                listOfStockists = (from s in db.Stockists
                                  join ps in db.ProductStocks on s.StockistID equals ps.StockistID
                                  where ps.ProductReferenceID == productID
                                  select s).ToList< Stockist>();
            }
            if (listOfStockists == null)
                return BadRequest();
            else if (listOfStockists.Count() > 0)
                return Ok(listOfStockists);
            else
                return NotFound();
        }
    }
}
