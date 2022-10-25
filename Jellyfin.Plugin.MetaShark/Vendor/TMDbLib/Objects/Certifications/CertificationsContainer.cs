using System.Collections.Generic;

namespace TMDbLib.Objects.Certifications
{
    public class CertificationsContainer
    {
        public Dictionary<string, List<CertificationItem>> Certifications { get; set; }
    }
}