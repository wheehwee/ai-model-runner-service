namespace Domain.Entities
{
    public abstract class BaseEntity<TKey> : IAuditable
    {
        protected BaseEntity()
        {
            CreationTime = DateTimeOffset.UtcNow;
        }

        public TKey Id { get; set; }

        public HashSet<string> RecentlyModifiedFields { get; set; } = new HashSet<string>();

        public DateTimeOffset CreationTime { get; set; }

        public DateTimeOffset? LastModificationTime { get; set; }

        public bool Deleted { get; set; } = false;

        public DateTimeOffset? DeletionTime { get; private set; }

        public void Delete()
        {
            if (Deleted)
            {
                return;
            }

            Deleted = true;
            DeletionTime = DateTimeOffset.UtcNow;
        }
    }

    public interface IAuditable
    {
        public DateTimeOffset CreationTime { get; set; }

        public DateTimeOffset? LastModificationTime { get; set; }
    }
}
