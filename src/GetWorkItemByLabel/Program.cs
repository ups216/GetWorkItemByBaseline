using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace GetWorkItemByLabel
{
    class Program
    {
        static void Main(string[] args)
        {
            // TFS团队项目集合URL，比如：http://tfsserver:8080/DefaultCollection
            string tfsCollectionUrl = "";
            // 个人访问令牌 https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=vsts
            string personalAccessToken = "";

            GetWorkItem(tfsCollectionUrl, personalAccessToken, 114);            
            GetChangesetWorkItemFromLabel(tfsCollectionUrl, personalAccessToken);

        }

        static public void GetWorkItemFromGitTags(string tfsCollectionUrl, string personalAccessToken, string baseTag, string targetTag)
        {
            VssBasicCredential credentials = new VssBasicCredential(string.Empty, personalAccessToken);
            VssConnection connection = new VssConnection(new Uri(tfsCollectionUrl), credentials);

            GitHttpClient gitHttpClient = connection.GetClient<GitHttpClient>();

            GitBaseVersionDescriptor baseVersion = new GitBaseVersionDescriptor();
        }

        static public void GetWorkItem(string tfsCollectionUrl, string personalAccessToken, int id)
        {
            Console.WriteLine("Getting WorkItem ID "+id);

            VssBasicCredential credentials = new VssBasicCredential(string.Empty, personalAccessToken);
            VssConnection connection = new VssConnection(new Uri(tfsCollectionUrl), credentials);

            WorkItemTrackingHttpClient workItemTrackingHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();

            WorkItem workitem = workItemTrackingHttpClient.GetWorkItemAsync(id).Result;
            

            Console.WriteLine(workitem.Fields["System.Title"]);

            Console.ReadLine();
        }

        static public void GetChangesetWorkItemFromLabel(string tfsCollectionUrl, string personalAccessToken)
        {
            Console.WriteLine("Loading Labels Items and associated work items ... ");

            VssBasicCredential credentials = new VssBasicCredential(string.Empty, personalAccessToken);

            using (TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(tfsCollectionUrl), credentials))
            {
                // Can retrieve SOAP service from TfsTeamProjectCollection instance
                //VersionControlServer vcServer = tpc.GetService<VersionControlServer>();
                //ItemSet itemSet = vcServer.GetItems("$/", RecursionType.OneLevel);
                //foreach (Item item in itemSet.Items)
                //{
                //    Console.WriteLine(item.ServerItem);
                //}

                // Can retrieve REST client from same TfsTeamProjectCollection instance
                TfvcHttpClient tfvcClient = tpc.GetClient<TfvcHttpClient>();
                //List<TfvcItem> tfvcItems = tfvcClient.GetItemsAsync("$/", VersionControlRecursionType.OneLevel).Result;
                //foreach (TfvcItem item in tfvcItems)
                //{
                //    Console.WriteLine(item.Path);
                //}

                TfvcLabelRequestData tfvcLabelRequestData = new TfvcLabelRequestData();
                
                List<TfvcLabelRef> labelRefs = tfvcClient.GetLabelsAsync(tfvcLabelRequestData).Result;

                foreach(TfvcLabelRef item in labelRefs)
                {
                    Console.WriteLine("Label Id: " + item.Id + "\tName: " + item.Name + "\t Modified:" + item.ModifiedDate);
                    TfvcLabel tfvcLabel = tfvcClient.GetLabelAsync(item.Id.ToString(), tfvcLabelRequestData).Result;
                    List<TfvcItem> labelItems = tfvcClient.GetLabelItemsAsync(item.Id.ToString()).Result;
                    foreach(TfvcItem vcItem in labelItems)
                    {
                        Console.WriteLine("\tItem:" + vcItem.Path + "\t ChangesetVersion: " + vcItem.ChangesetVersion);

                        List<AssociatedWorkItem> changesetWorkItems = tfvcClient.GetChangesetWorkItemsAsync(vcItem.ChangesetVersion).Result;
                        foreach(AssociatedWorkItem assItem in changesetWorkItems)
                        {
                            Console.WriteLine("\t\t Associated WorkItem Id: " + assItem.Id + "\t Title: " + assItem.Title);
                        }
                    }
                }
            }

            Console.ReadLine();
        }


    }
}
