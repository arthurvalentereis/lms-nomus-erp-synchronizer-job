namespace lms_nomus_erp_synchronizer_job.Domain.Models
{
    public class UserGroupConfiguration
    {
        public string CreditorDocument { get; set; }
        public long UserGroupId { get; set; }
        public long UserCompanyId { get; set; }
        public string TokenUser { get; set; }
        public string UrlUser { get; set; }
    }
}
