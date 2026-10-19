namespace lms_nomus_erp_synchronizer_job.Domain.Models
{
    public class UserGroupConfiguration
    {
        public string CreditorDocument { get; set; }
        public long UserGroupId { get; set; }
        public long UserCompanyId { get; set; }
        public string TokenUser { get; set; }
        public string UrlUser { get; set; }
        public bool Run { get; set; }

        /// <summary>
        /// Quando <see cref="Run"/> é true: se false, sync D-1 do UserGroupId configurado;
        /// se true, carga de histórico completo.
        /// </summary>
        public bool FullHistory { get; set; }
    }
}
