using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VisionWinML
{
    class VisitorCmds
    {
        public class FaceRectangle
        {
            public int top { get; set; }
            public int left { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class Visitors
        {
            public string faceId { get; set; }
            public FaceRectangle faceRectangle { get; set; }
        }

        public class FaceQueryPayload
        {
            public string personGroupId { get; set; }
            public List<string> faceIds { get; set; }
            public int maxNumOfCandidatesReturned { get; set; }
            public double confidenceThreshold { get; set; }
        }


        public class Candidate
        {
            public string personId { get; set; }
            public double confidence { get; set; }
        }

        public class CandidateObject
        {
            public string faceId { get; set; }
            public List<Candidate> candidates { get; set; }
        }

        //public static async Task<List<string>> CheckVisitorFace(string responseString, string personGroupId)
        public static async Task<List<string>> CheckVisitorFace(string responseString, List<PersonGroups> items)
        {
            List<Visitors> visitors = JsonConvert.DeserializeObject<List<Visitors>>(responseString);
            List<string> faceIds = new List<string>();
            List<string> names = new List<string>();

            foreach (Visitors visitor in visitors)
            {
                faceIds.Add(visitor.faceId);
            }

            foreach (var item in items.Skip(1))
            {
                string personGroupId = item.personGroupId;
                string personGroupName = item.name;

                FaceQueryPayload jsonPayLoad = new FaceQueryPayload();
                jsonPayLoad.personGroupId = personGroupId;
                jsonPayLoad.faceIds = faceIds;
                jsonPayLoad.maxNumOfCandidatesReturned = 1;
                jsonPayLoad.confidenceThreshold = 0.5;


                string uri = "https://api.projectoxford.ai/face/v1.0/identify";
                string jsonString = JsonConvert.SerializeObject(jsonPayLoad);
                HttpContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await HttpHandler.client.PostAsync(uri, content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    List<CandidateObject> candidates = JsonConvert.DeserializeObject<List<CandidateObject>>(responseBody);

                    foreach (CandidateObject candidate in candidates)
                    {
                        //get person from personId
                        if (candidate.candidates.Count != 0)
                        {
                            string name = await PersonsCmds.GetPerson(personGroupId, personGroupName, candidate.candidates[0].personId);
                            if (null != name)
                            {
                                names.Add(name);
                                faceIds.Remove(candidate.faceId);
                            }

                        }
                    }
                }
            }
            return names;
        }
    }
}
