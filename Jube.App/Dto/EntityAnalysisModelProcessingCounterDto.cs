using System;

namespace Jube.App.Dto
{
    public class EntityAnalysisModelProcessingCounterDto
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Instance { get; set; }
        public int ModelInvoke { get; set; }
        public int GatewayMatch { get; set; }
        public Guid EntityAnalysisModelGuid { get; set; }
        public int ResponseElevation { get; set; }
        public double ResponseElevationSum { get; set; }
        public double ActivationWatcher { get; set; }
        public int ResponseElevationValueLimit { get; set; }
        public int ResponseElevationLimit { get; set; }
        public int ResponseElevationValueGatewayLimit { get; set; }
    }
}