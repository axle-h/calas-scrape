using Calas.Scrape.Attributes;

namespace Calas.Scrape.Models
{
    public class Enquiry
    {
        [DataField("ID")]
        public string Id { get; set; }

        public string ContactId { get; set; }

        [DataField("Finance type")]
        public string FinanceType { get; set; }

        [DataField("Vehicle")]
        public string Vehicle { get; set; }

        [DataField("State")]
        public string State { get; set; }

        [DataField("Status changed")]
        public string StatusChanged { get; set; }

        [DataField("Renewal date")]
        public string RenewalDate { get; set; }

        [DataField("Term")]
        public string Term { get; set; }

        [DataField("Mileage")]
        public string Mileage { get; set; }

        [DataField("Maintenance")]
        public string Maintenance { get; set; }

        [DataField("Lead time")]
        public string LeadTime { get; set; }

        [DataField("Vehicle contact")]
        public string VehicleContact { get; set; }

        [DataField("VRM")]
        public string VRM { get; set; }
    }
}