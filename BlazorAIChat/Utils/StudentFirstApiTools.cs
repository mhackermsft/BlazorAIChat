using DocumentFormat.OpenXml.Spreadsheet;

namespace BlazorAIChat.Utils;

public static class StudentFirstApiTools
{
    // usa-gold
    private static string apiKey = "e28f3723-7e2c-4e26-a299-b3a5664505ac";
    private static string geturl = "https://sisstagingapi.studentfirst.app/systemconfiguration/ReferenceTypes/MaritalStatuses?activeOnly=false";
    private static string postUrl = "https://sisstagingapi.studentfirst.app/systemconfiguration/DataManagement/JsonUpload/";
    private static string apiUser = "bob.miller@studentfirst.com";
    private static int institutionId = 211;

    // - development
    //private static string apiKey = "84819a6d-e822-4f35-8cfa-ac2da423007d";
    //private static string geturl = "https://localhost:44397/ReferenceTypes/MaritalStatuses?activeOnly=false";
    //private static string postUrl = "https://localhost:44397/DataManagement/JsonUpload/";
    //private static string apiUser = "bob.miller@studentfirst.com";
    //private static int institutionId = 1;




    public static async Task<string> GetMaritalStatuses()
    {
        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-api-Key", apiKey);
        client.DefaultRequestHeaders.Add("x-api-username", apiUser);
        client.DefaultRequestHeaders.Add("x-institution", institutionId.ToString());

        var response = await client.GetAsync(geturl);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        return responseString;
    }

    public static async Task SaveResults(string itemName, string content)
    {
        //HttpClient client = new HttpClient();
        //client.DefaultRequestHeaders.Add("x-api-Key", apiKey);
        //client.DefaultRequestHeaders.Add("x-api-username", apiUser);
        //client.DefaultRequestHeaders.Add("x-institution", institutionId.ToString());
        //client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
        //var response = await client.PostAsJsonAsync($"{postUrl}{itemName.Replace(" ", "_")}", new { Data = content });
        //response.EnsureSuccessStatusCode();
        await Task.Delay(100);
    }
}

