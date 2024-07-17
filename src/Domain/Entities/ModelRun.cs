using Domain.Enums;

namespace Domain.Entities
{
    public abstract class ModelRun : BaseEntity<string>
    {
        public ProcessingState State { get; set; }
        public abstract ModelRunType ModelRunType { get; set; }
    }
}
