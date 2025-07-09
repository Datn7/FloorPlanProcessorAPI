namespace FloorPlanProcessorAPI.Models
{
    public class FloorPlan
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FileName { get; set; }
        public string SvgPath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
