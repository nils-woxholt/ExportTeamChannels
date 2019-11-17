namespace QuickType
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    public partial class Channels
    {
        [JsonProperty("@odata.context")]
        public Uri OdataContext { get; set; }

        [JsonProperty("value")]
        public List<Channel> Value { get; set; }
    }

    public partial class Channel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("isFavoriteByDefault")]
        public string IsFavoriteByDefault { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("webUrl")]
        public Uri WebUrl { get; set; }

        [JsonProperty("membershipType")]
        public string MembershipType { get; set; }
    }

}
