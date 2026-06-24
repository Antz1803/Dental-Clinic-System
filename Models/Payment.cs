using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCAS.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        public decimal Cash { get; set; }
        public string DoctorName { get; set; }


        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountChanged { get; set; }
        public decimal DentistFee { get; set; }
        public DateTime PaymentDate { get; set; }


        // Foreign key for PersonInfo
        public int PersonInfoId { get; set; }

        [ForeignKey("PersonInfoId")]
        public PersonInfo Person { get; set; }

        // Foreign key for Services
        public int ServicesId { get; set; }

        [ForeignKey("ServicesId")]
        public Services Service { get; set; }
        public ICollection<PaymentMedicine> PaymentMedicine { get; set; } = new List<PaymentMedicine>();
    }
}
