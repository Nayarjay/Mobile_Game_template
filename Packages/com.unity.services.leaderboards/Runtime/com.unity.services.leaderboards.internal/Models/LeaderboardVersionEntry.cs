using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Leaderboards.Internal.Http;

namespace Unity.Services.Leaderboards.Internal.Models
{
    /// <summary>
    /// LeaderboardVersionEntry model
    /// </summary>
    [Preserve]
    [DataContract(Name = "LeaderboardVersionEntry")]
    internal class LeaderboardVersionEntry
    {
        /// <summary>
        /// Creates an instance of LeaderboardVersionEntry.
        /// </summary>
        /// <param name="playerId">playerId param</param>
        /// <param name="playerName">playerName param</param>
        /// <param name="rank">rank param</param>
        /// <param name="score">score param</param>
        /// <param name="version">version param. Optional; defaults to default</param>
        /// <param name="metadata">metadata param. Optional; defaults to null</param>
        [Preserve]
        public LeaderboardVersionEntry(string playerId, string playerName, int rank, double score, LeaderboardVersion version = default, object metadata = null)
        {
            Version = version;
            PlayerId = playerId;
            PlayerName = playerName;
            Rank = rank;
            Score = score;
            Metadata = metadata;
        }

        /// <summary>
        /// Parameter version of LeaderboardVersionEntry
        /// </summary>
        [Preserve]
        [DataMember(Name = "version", EmitDefaultValue = false)]
        public LeaderboardVersion Version{ get; }

        /// <summary>
        /// Parameter playerId of LeaderboardVersionEntry
        /// </summary>
        [Preserve]
        [DataMember(Name = "playerId", IsRequired = true, EmitDefaultValue = true)]
        public string PlayerId{ get; }

        /// <summary>
        /// Parameter playerName of LeaderboardVersionEntry
        /// </summary>
        [Preserve]
        [DataMember(Name = "playerName", IsRequired = true, EmitDefaultValue = true)]
        public string PlayerName{ get; }

        /// <summary>
        /// Parameter rank of LeaderboardVersionEntry
        /// </summary>
        [Preserve]
        [DataMember(Name = "rank", IsRequired = true, EmitDefaultValue = true)]
        public int Rank{ get; }

        /// <summary>
        /// Parameter score of LeaderboardVersionEntry
        /// </summary>
        [Preserve]
        [DataMember(Name = "score", IsRequired = true, EmitDefaultValue = true)]
        public double Score{ get; }

        /// <summary>
        /// Parameter updatedTime of LeaderboardEntry
        /// </summary>
        [Preserve]
        [DataMember(Name = "metadata", EmitDefaultValue = false)]
        public object Metadata { get; }
    }
}
