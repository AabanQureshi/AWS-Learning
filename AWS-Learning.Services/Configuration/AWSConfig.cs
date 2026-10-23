using System.ComponentModel.DataAnnotations;

namespace AWS_Learning.Configuration
{
    public class AWSConfig
    {
        [Required]
        public string ServiceUrl { get; set; } = string.Empty;
        [Required]
        public string Region { get; set; } = string.Empty;
        [Required]
        public string ApiProfile { get; set; } = string.Empty;
        [Required]
        public string WorkerProfile { get; set; } = string.Empty;
        [Required]
        public string BucketName { get; set; } = string.Empty;
        [Required]
        public string JobsTableName { get; set; } = string.Empty;
        [Required]
        public string QueueUrl { get; set; } = string.Empty;

    }
}
