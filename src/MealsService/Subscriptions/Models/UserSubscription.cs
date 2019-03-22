using System;

namespace MealsService.Subscriptions.Models
{
    public class UserSubscription
    {
        public int UserId { get; set; }
        public string SubscriptionId { get; set; }
        public DateTime? Expiration { get; set; }
        public bool AutoRenew { get; set; }

        public SubscriptionStatus Status { get; set; }
    }

    public enum SubscriptionStatus
    {
        NONE = 0,
        TRIAL = 1,
        ACTIVE = 2,
        EXPIRED = 3
    }
}
