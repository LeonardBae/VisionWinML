using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VisionWinML
{
    public class Persons
    {
        public string personId { get; set; }
        public string name { get; set; }
        public string userData { get; set; }
        public List<string> persistedFaceIds { get; set; }

        public Persons(string v1, string v2, string v3, List<string> v4)
        {
            personId = v1;
            name = v2;
            userData = v3;
            persistedFaceIds = v4;
        }

    }
    public class PersonsCmds
    {
        // https://dev.projectoxford.ai/docs/services/563879b61984550e40cbbe8d/operations/563879b61984550f30395241
        // List, Create, Get, Delete

        public static async Task<List<Persons>> ListPersonInGroup(string personGroupId)
        {
            List<Persons> persons = null;
            string uri = HttpHandler.BaseUri + "/" + personGroupId + "/persons";
            HttpResponseMessage response = await HttpHandler.client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                persons = JsonConvert.DeserializeObject<List<Persons>>(responseBody);
                persons.Insert(0, new Persons("...", "...", "...", new List<string> { "..." }));
            }
            else
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                globals.ShowJsonErrorPopup(responseBody);
            }
            return persons;
        }

        public static async Task<string> CreatePerson(string personGroupId, string name, string userData)
        {
            string uri = HttpHandler.BaseUri + "/" + personGroupId + "/persons";
            string jsonString = "{\"name\":\"" + name + "\", \"userData\":\"" + userData + "\"}";
            HttpContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await HttpHandler.client.PostAsync(uri, content);
            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                globals.ShowJsonErrorPopup(responseBody);
            }
            return response.ToString();
        }

        public static async Task<string> UpdatePerson(string personGroupId, string personId, string name, string userData)
        {
            string responseBody = null;
            string uri = HttpHandler.BaseUri + "/" + personGroupId + "/persons/" + personId;
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

        public static async Task<string> DeletePerson(string personGroupId, string personId)
        {
            string responseBody = null;
            string uri = HttpHandler.BaseUri + "/" + personGroupId + "/persons/" + personId;
            HttpResponseMessage response = await HttpHandler.client.DeleteAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                responseBody = await response.Content.ReadAsStringAsync();
                globals.ShowJsonErrorPopup(responseBody);
            }
            return responseBody;
        }

        public static async Task<string> GetPerson(string personGroupId, string personGroupName, string personId)
        {
            string responseBody = null;
            Persons person = null;
            string uri = HttpHandler.BaseUri + "/" + personGroupId + "/persons/" + personId;
            HttpResponseMessage response = await HttpHandler.client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                responseBody = await response.Content.ReadAsStringAsync();
                globals.ShowJsonErrorPopup(responseBody);
                return person.ToString();
            }

            responseBody = await response.Content.ReadAsStringAsync();
            person = JsonConvert.DeserializeObject<Persons>(responseBody);

            return person.name + " [" + personGroupName + ": " + person.userData + "]";
        }

    }
}
