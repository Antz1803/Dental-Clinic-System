using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCAS.Models
{
    public class PaymentMedicine
    {
        [Key]
        public int PaymentMedicineId { get; set; }
        public string MedicineName{ get; set; }
        // Foreign key for Payment
        public int PaymentId { get; set; }

        // Price at the time of the transaction
        public decimal Price { get; set; }

        public virtual Payment Payment { get; set; }
    }
}