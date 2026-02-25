namespace lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs
{
    public class RequestCustomerDto
    {
        public long? UserGroupId { get; set; }
        public long? UserCompanyId { get; set; }
        public decimal? CreditLimit { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
        public string Neighborhood { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Complement { get; set; } = string.Empty;
       
    }
}
