using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MinimaxAPISample
{
    class Program
    {
        static void Main(string[] args)
        {
            // access token
            var accessToken = Common.AutGetAccessToken(Config.ApiTokenEndpoint, Config.ClientId, Config.ClientSecret, Config.UserName, Config.Password);

            // organisation list
            var json = Common.ApiGet("api/currentuser/orgs", accessToken);
            var IDs = json.SelectTokens("$..ID").Select(x => x.Value<string>()).ToList();
            string organisationId = organisationId = IDs[0]; // take first org on the list

            //organisationId = "zzz";

            // get connected ids
            string currencyCode = "EUR";
            string vatRateCode = "S";
            string date = DateTime.Now.ToString("yyyy-MM-dd"); ;
            string itemCode = Guid.NewGuid().ToString().Substring(0, 30);
            string countryCode = "HU";
            string customerCode = Guid.NewGuid().ToString().Substring(0, 30);
            string IRreportTemplateCode = "IR";
            string DOreportTemplateCode = "DO";

            json = Common.ApiGet($"api/orgs/{organisationId}/vatrates/code({vatRateCode})?date={date}", accessToken);
            string vatRateId = (string)json["VatRateId"];
            var vatPercent = (string)json["Percent"];

            json = Common.ApiGet($"api/orgs/{organisationId}/currencies/code({currencyCode})", accessToken);
            string currencyId = ((long)json["CurrencyId"]).ToString();

            json = Common.ApiGet($"api/orgs/{organisationId}/countries/code({countryCode})", accessToken);
            string countryId = ((long)json["CountryId"]).ToString();

            json = Common.ApiGet($"api/orgs/{organisationId}/report-templates?SearchString={IRreportTemplateCode}&PageSize=100", accessToken);
            var IRReportTemplate = ((JArray)json["Rows"]).Where(element => (string)element["DisplayType"] == IRreportTemplateCode).First();
            var IRreportTemplateId = (string)IRReportTemplate["ReportTemplateId"];

            json = Common.ApiGet($"api/orgs/{organisationId}/report-templates?SearchString={DOreportTemplateCode}&PageSize=100", accessToken);
            var DOReportTemplate = ((JArray)json["Rows"]).Where(element => (string)element["DisplayType"] == DOreportTemplateCode).First();
            var DOreportTemplateId = (string)DOReportTemplate["ReportTemplateId"];

            // add item

            string jsonStr = $@"
            {{
                Name: ""Test"",
                Code: ""{itemCode}"",
                ItemType: ""B"",
                VatRate: {{ID: {vatRateId} }},
                Price: 100.0,
                Currency: {{ID: {currencyId} }}
            }}";

            json = Common.ApiPost($"api/orgs/{organisationId}/items", accessToken, jsonStr);

            string itemId = ((long)json["ItemId"]).ToString();

            // add customer

            jsonStr = $@"
            {{
                Name: ""Test"",
                Address: ""test"",
                PostalCode: ""1234"",
                City: ""Nowhere"",
                Code: ""{customerCode}"",
                Country: {{ID: {countryId} }},
                CountryName: ""-"",
                SubjectToVAT: ""N"",
                Currency: {{ID: {currencyId} }},
                EInvoiceIssuing: ""SeNePripravlja""
            }}";

            json = Common.ApiPost($"api/orgs/{organisationId}/customers", accessToken, jsonStr);

            string customerId = ((long)json["CustomerId"]).ToString();

            // add invoice

            var employeeBlock = "";

            if (Config.Lokalizacija == "HR")
            {
                string employeeString = "Test";
                json = Common.ApiGet($"api/orgs/{organisationId}/employees?SearchString={employeeString}", accessToken);
                string employeeId = (string)json["Rows"][0]["EmployeeId"];

                employeeBlock = $@"
                   Employee:{{
		                ID:{employeeId}
                   }},";
            }

            jsonStr = $@"
            {{
               Customer:{{
		            ID:{customerId}
               }},
               {employeeBlock}
               DateIssued:""{date}"",
               DateTransaction:""{date}"",
               DateTransactionFrom:""{date}"",
               DateDue:""{date}"",
               AddresseeName:""Test"",
               AddresseeAddress:""Nowhere"",
               AddresseePostalCode:""1234"",
               AddresseeCity:""Test"",
               AddresseeCountryName :""-"",
               AddresseeCountry:{{
                  ID:{countryId}
               }},
               Currency:{{
                  ID:{currencyId}
               }},
               IssuedInvoiceReportTemplate:{{
                  ID:{IRreportTemplateId}
               }},
               DeliveryNoteReportTemplate:{{
                  ID:{DOreportTemplateId}
               }},
               Status:""O"",
               PricesOnInvoice:""N"",
               RecurringInvoice:""N"",
               InvoiceType:""R"",
               PaymentStatus:""Placan"",
               IssuedInvoiceRows:[
                  {{
                     Item:{{
                        ID:{itemId}
                     }},
                     ItemName:""Test"",
                     RowNumber:1,
                     ItemCode:""code"",
                     Description:""description"",
                     Quantity:1,
                     UnitOfMeasurement:""kom"",
                     Price:10.6475,
                     PriceWithVAT:12.99,
                     VATPercent:{vatPercent},
                     Discount:0,
                     DiscountPercent:0,
                     Value:10.6475,
                     VatRate:{{
                        ID:{vatRateId}
                     }}
                  }}
               ]
            }}";

            json = Common.ApiPost($"api/orgs/{organisationId}/issuedinvoices", accessToken, jsonStr);

            string issuedInvoiceId = ((long)json["IssuedInvoiceId"]).ToString();
            string status = (string)json["Status"];
            string actionName = "IssueAndGeneratePdf";
            string rowVersion = ((string)json["RowVersion"]);

            // issue invoice and generate pdf

            json = Common.ApiPut($"api/orgs/{organisationId}/issuedinvoices/{issuedInvoiceId}/actions/{actionName}?rowVersion={rowVersion}", accessToken, string.Empty);

            var attachmentId = (long)json["Data"]["AttachmentId"];
            var attachmentData = (string)json["Data"]["AttachmentData"];
            var attachmentDate = (string)json["Data"]["AttachmentDate"];
            var attachmentFileName = (string)json["Data"]["AttachmentFileName"];
            var attachmentMimeType = (string)json["Data"]["AttachmentMimeType"];

            byte[] pdfbytes = Convert.FromBase64String(attachmentData);
            File.WriteAllBytes(attachmentFileName, pdfbytes); // save invoice pdf to disk
        }
    }
}
