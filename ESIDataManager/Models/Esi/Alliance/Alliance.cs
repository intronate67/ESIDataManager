﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECT.Models.ESI.Alliance
{
    public class Alliance 
    {
        [JsonProperty("creator_corporation_id")]
        public int CreatorCorporationId { get; set; }

        [JsonProperty("creator_id")]
        public int CreatorId { get; set; }

        [JsonProperty("date_founded")]
        public DateTime DateFounded { get; set; }

        [JsonProperty("executor_corporation_id")]
        public int? ExecutorCorporationId { get; set; }

        [JsonProperty("faction_id")]
        public int? FactionId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ticker")]
        public string Ticker { get; set; }
    }
}
