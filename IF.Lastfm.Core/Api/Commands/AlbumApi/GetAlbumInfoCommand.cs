﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Api.Helpers;
using IF.Lastfm.Core.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IF.Lastfm.Core.Api.Commands.AlbumApi
{
    internal class GetAlbumInfoCommand : GetAsyncCommandBase<LastResponse<Album>>
    {
        public string ArtistName { get; private set; }
        public string AlbumName { get; private set; }
        public bool Autocorrect { get; set; }

        public GetAlbumInfoCommand(IAuth auth, string artistname, string albumname) : base(auth)
        {
            Method = "album.getInfo";
            ArtistName = artistname;
            AlbumName = albumname;
        }

        public override Uri BuildRequestUrl()
        {
            var parameters = new Dictionary<string, string>
                {
                    {"artist", Uri.EscapeDataString(ArtistName)},
                    {"album", Uri.EscapeDataString(AlbumName)},
                    {"autocorrect", Convert.ToInt32(Autocorrect).ToString()}
                };

            base.DisableCaching(parameters);

            var apiUrl = LastFm.FormatApiUrl(Method, Auth.ApiKey, parameters);
            return new Uri(apiUrl, UriKind.Absolute);
        }

        public async override Task<LastResponse<Album>> HandleResponse(HttpResponseMessage response)
        {
            string json = await response.Content.ReadAsStringAsync();

            LastFmApiError error;
            if (LastFm.IsResponseValid(json, out error) && response.IsSuccessStatusCode)
            {
                var jtoken = JsonConvert.DeserializeObject<JToken>(json);

                var album = Album.ParseJToken(jtoken.SelectToken("album"));

                return LastResponse<Album>.CreateSuccessResponse(album);
            }
            else
            {
                return LastResponse<Album>.CreateErrorResponse(error);
            }
        }
    }

}
