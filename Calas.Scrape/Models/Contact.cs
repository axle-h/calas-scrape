using System.Collections.Generic;
using Calas.Scrape.Attributes;
using CsvHelper.Configuration.Attributes;

namespace Calas.Scrape.Models
{
    public class Contact
    {
        [DataField("ID")]
        public string Id { get; set; }

        [DataField("Date")]
        public string Date { get; set; }

        [DataField("Title")]
        public string Title { get; set; }

        [DataField("First name")]
        public string FirstName { get; set; }

        [DataField("Surname")]
        public string Surname { get; set; }

        [DataField("Telephone")]
        public string Telephone { get; set; }

        [DataField("Email")]
        public string Email { get; set; }

        [DataField("Company")]
        public string Company { get; set; }

        [DataField("Type")]
        public string Type { get; set; }

        [DataField("Postcode")]
        public string Postcode { get; set; }

        [DataField("cont_address1", DataFieldType.Form)]
        public string AddressLine1 { get; set; }

        [DataField("cont_address2", DataFieldType.Form)]
        public string AddressLine2 { get; set; }

        [DataField("cont_address3", DataFieldType.Form)]
        public string AddressLine3 { get; set; }

        [DataField("cont_address4", DataFieldType.Form)]
        public string Town { get; set; }

        [DataField("cont_address5", DataFieldType.Form)]
        public string County { get; set; }

        [DataField("cont_website", DataFieldType.Form)]
        public string Website { get; set; }

        [Ignore]
        public ICollection<Enquiry> Enquiries { get; set; }
    }
}
