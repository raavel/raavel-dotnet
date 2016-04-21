using System.Collections.Generic;
using Newtonsoft.Json;

namespace Raavel.Payload
{
    /// <summary>
    /// Represents a single notification object to send to Raavel
    /// </summary>
    internal class Notification
    {
        /// <summary>
        /// Gets or sets the API key associated with the Raavel account to send the notification to
        /// </summary>
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the information about the notifier used 
        /// </summary>
        [JsonProperty("notifier")]
        public NotifierInfo Notifier { get; set; }

        /// <summary>
        /// Gets or sets the list of events to send in the notification (normally one event)
        /// </summary>
        [JsonProperty("events")]
        public List<EventInfo> Events { get; set; }
    }
}
