using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Api.Helpers;
using IF.Lastfm.Core.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IF.Lastfm.Core.Api.Commands.UserApi
{
    internal class GetRecentScrobblesCommand : GetAsyncCommandBase<PageResponse<Track>>
    {
        public string Username { get; private set; }
        public DateTime From { get; private set; }

        public GetRecentScrobblesCommand(IAuth auth, string username, DateTime from) : base(auth)
        {
            Method = "user.getRecentTracks";
            Username = username;
            From = from;
        }

        public async override Task<PageResponse<Track>> ExecuteAsync()
        {
            var parameters = new Dictionary<string, string>
                             {
                                 {"user", Username},
                                 {"from", From.ToUnixTimestamp().ToString()}
                             };

            base.AddPagingParameters(parameters);

            var uristring = LastFm.FormatApiUrl(Method, Auth.ApiKey, parameters);
            Url = new Uri(uristring, UriKind.Absolute);

            return await ExecuteInternal();
        }

        public async override Task<PageResponse<Track>> HandleResponse(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();

            LastFmApiError error;
            if (LastFm.IsResponseValid(json, out error) && response.IsSuccessStatusCode)
            {
                JToken jtoken = JsonConvert.DeserializeObject<JToken>(json).SelectToken("recenttracks");

                var tracksToken = jtoken.SelectToken("track");

                var tracks = new List<Track>();
                foreach (var track in tracksToken.Children())
                {
                    var t = Track.ParseJToken(track);
                    var date = track.SelectToken("date");
                    if (date != null)
                    {
                        var stamp = date.Value<double>("uts");
                        t.TimePlayed = stamp.ToDateTimeUtc();
                    }

                    tracks.Add(t);
                }

                var pageresponse = PageResponse<Track>.CreateSuccessResponse(tracks);

                var attrToken = jtoken.SelectToken("@attr");
                pageresponse.AddPageInfoFromJToken(attrToken);

                return pageresponse;
            }
            else
            {
                return PageResponse<Track>.CreateErrorResponse(error);
            }
        }
    }
}