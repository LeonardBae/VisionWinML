using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Windows.UI.Xaml.Controls;
using System;

namespace VisionWinML
{
    public class TrainOperationResult
    {
        public string status { get; set; }
    }
    public class PersonGroups
    {
        public string personGroupId { get; set; }
        public string name { get; set; }

        public string userData { get; set; }
        public PersonGroups(string _Id, string _name, string _userData)
        {
            personGroupId = _Id;
            name = _name;
            userData = _userData;
        }

    }

    static class PersonGroupCmds
    {
        // 
        // API Documentation:
        // https://dev.projectoxford.ai/docs/services/563879b61984550e40cbbe8d/operations/563879b61984550f30395248
        // API Testing Console:
        // https://dev.projectoxford.ai/docs/services/563879b61984550e40cbbe8d/operations/563879b61984550f30395248/console
        //
        public static async Task<List<PersonGroups>> ListPersonGroups()
        {
            List<PersonGroups> personGroups = null;
            string uri = HttpHandler.BaseUri + "?top=1000";
            HttpResponseMessage response = await HttpHandler.client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                personGroups = JsonConvert.DeserializeObject<List<PersonGroups>>(responseBody);
                personGroups.Insert(0, new PersonGroups("...", "...", "..."));
            }
            else
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                globals.ShowJsonErrorPopup(responseBody);
            }
            return personGroups;
        }

        public static async Task<string> AddPersonGroups(string personGroupId, string name, string userData)
        {
            string uri = HttpHandler.BaseUri + "/" + personGroupId;
            string jsonString = "{\"name\":\"" + name + "\", \"userData\":\"" + userData + "\"}";
            HttpContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await HttpHandler.client.PutAsync(uri, content);
            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                globals.ShowJsonErrorPopup(responseBody);
            }

            return response.ToString();
        }

        public static async Task<string> UpdatePersonGroups(string personGroupId, string name, string userData)
        {
            string responseBody = null;
            string uri = HttpHandler.BaseUri + "/" + personGroupId;
            string jsonString = "{\"name\":\"" + name + "\", \"userData\":\"" + userData + "\"}";

            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), uri)
            {
                Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
            };
            HttpResponseMessage response = await HttpHandler.client.SendAsync(request);// (uri, content);
            if (!response.IsSuccessStatusCode)
            {
                responseBody = await response.Content.ReadAsStringAsync();
                globals.ShowJsonErrorPopup(responseBody);
            }
            return responseBody;
        }

        public static async Task<string> DeletePersonGroups(string personGroupId)
        {
            string responseBody = null;
            string uri = HttpHandler.BaseUri + "/" + personGroupId;
            HttpResponseMessage response = await HttpHandler.client.DeleteAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                responseBody = await response.Content.ReadAsStringAsync();
                globals.ShowJsonErrorPopup(responseBody);
            }
            return responseBody;
        }

        public static async Task<string> TrainPersonGroups(string personGroupId)
        {
            string uri = HttpHandler.BaseUri + "/" + personGroupId + "/train";
            var response = await HttpHandler.client.PostAsync(new Uri(HttpHandler.BaseUri + "/" + personGroupId + "/train"), null);
            TrainOperationResult trainOperationResult = null;
            string status = "";
            HttpResponseMessage results = null;
            while (!status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                results = await HttpHandler.client.GetAsync(new Uri(HttpHandler.BaseUri + "/" + personGroupId + "/training"));
                var operationResult = await results.Content.ReadAsStringAsync();
                trainOperationResult = JsonConvert.DeserializeObject<TrainOperationResult>(operationResult);
                status = trainOperationResult.status;
            }
            return status;
        }

        public static async Task<string> StatusPersonGroups(string personGroupId)
        {
            string responseBody = null;
            string uri = HttpHandler.BaseUri + "/" + personGroupId + "/training";
            HttpResponseMessage response = await HttpHandler.client.GetAsync(uri);
            responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                globals.ShowJsonErrorPopup(responseBody);
                responseBody = "";
            }
            return responseBody;
        }
    }
}
