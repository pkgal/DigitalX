//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace StockWebApi.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ProductStock
    {
        public int StockID { get; set; }
        public decimal ProductReferenceID { get; set; }
        public int StockistID { get; set; }
        public int Total { get; set; }
    
        public virtual Stockist Stockist { get; set; }
    }
}
