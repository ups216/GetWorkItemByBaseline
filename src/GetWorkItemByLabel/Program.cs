using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
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
            string tfsCollectionUrl = args[0].ToString();
            // 个人访问令牌 https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=vsts
            string personalAccessToken = args[1].ToString();

            //GetWorkItem(tfsCollectionUrl, personalAccessToken, 114);            
            //GetChangesetWorkItemFromLabel(tfsCollectionUrl, personalAccessToken);
            Git_GetCommitLinkedWorkItemFromTags(args[0], args[1]);


        }

        static public void Git_GetCommitLinkedWorkItemFromTags(string tfsCollectionUrl, string personalAccessToken)
        {
            VssBasicCredential credentials = new VssBasicCredential(string.Empty, personalAccessToken);
            VssConnection connection = new VssConnection(new Uri(tfsCollectionUrl), credentials);
            WorkItemTrackingHttpClient workItemTrackingHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();


            string projectName = "tfvctest01";
            string commitDiffRepo = "repo01";
            string pullRequestDiffRepo = "repo02";

            GitHttpClient gitHttpClient = connection.GetClient<GitHttpClient>();
            ApiResourceLocation location = new ApiResourceLocation();


            List<GitRepository> repoList = gitHttpClient.GetRepositoriesAsync(projectName).Result;

            foreach (GitRepository repo in repoList)
            {
                if (repo.Name == commitDiffRepo)
                {
                    //场景1：在MASTER分支上使用两个TAG之间的COMMIT LIST获取相关工作项

                    //
                    // https://stackoverflow.com/questions/5863426/get-commit-list-between-tags-in-git
                    // 参考以上链接，先用 git logs --online TAG1...TAG2 命令获取到2个TAG之间的所有COMMIT ID，然后再用以下方式获取相关的WORK ITEM
                    // 找不到一个可以用的REST API可以做到以上命令做的事情
                    //

                    string baseline20181106 = "refs/tags/BASELINE-20181106-3";
                    string baseline000 = "refs/tags/BASELINE-000";
                    string baseline001 = "refs/tags/BASELINE-001";
                    string baseline002 = "refs/tags/BASELINE-002";
                

                    List<GitRef> tagRefs = gitHttpClient.GetTagRefsAsync(repo.Id).Result;

                    GitAnnotatedTag annotedTagBaseline20181106 = gitHttpClient.GetAnnotatedTagAsync(
                        projectName,
                        repo.Id,
                        (tagRefs.Find(x => x.Name == baseline20181106)).ObjectId
                        ).Result;

                    GitAnnotatedTag annotedTagBaseline000 = gitHttpClient.GetAnnotatedTagAsync(
                        projectName, 
                        repo.Id, 
                        (tagRefs.Find(x => x.Name == baseline000)).ObjectId
                        ).Result;
                    GitAnnotatedTag annotedTagBaseline001 = gitHttpClient.GetAnnotatedTagAsync(
                        projectName,
                        repo.Id,
                        (tagRefs.Find(x => x.Name == baseline001)).ObjectId
                        ).Result;
                    GitAnnotatedTag annotedTagBaseline002 = gitHttpClient.GetAnnotatedTagAsync(
                       projectName,
                       repo.Id,
                       (tagRefs.Find(x => x.Name == baseline002)).ObjectId
                       ).Result;


                    //
                    //Diff获取的结果是两个commit之间的差异文件列表，不是我们要的，我们需要的是2个commit之间的commit列表
                    //
                    /*
                    GitBaseVersionDescriptor baseVersion = new GitBaseVersionDescriptor();
                    baseVersion.Version = annotedTagBaseline000.TaggedObject.ObjectId;
                    baseVersion.VersionType = GitVersionType.Commit;
                    GitTargetVersionDescriptor targetVersion = new GitTargetVersionDescriptor();
                    targetVersion.Version = annotedTagBaseline001.TaggedObject.ObjectId;
                    targetVersion.VersionType = GitVersionType.Commit;
                    GitCommitDiffs commitDiffsResult = gitHttpClient.GetCommitDiffsAsync(repo.Id, false, null, null, baseVersion, targetVersion).Result;
                    */

                    GitCommit fromCommit = gitHttpClient.GetCommitAsync(annotedTagBaseline20181106.TaggedObject.ObjectId, repo.Id).Result;
                    GitCommit toCommit = gitHttpClient.GetCommitAsync(annotedTagBaseline000.TaggedObject.ObjectId, repo.Id).Result;

                    GitQueryCommitsCriteria criteria = new GitQueryCommitsCriteria()
                    {
                        IncludeLinks = true,
                        IncludeWorkItems = true
                    };

                    List<GitCommitRef> commits = gitHttpClient.GetCommitsAsync(repo.Id, criteria).Result;

                    foreach (GitCommitRef commitRef in commits)
                    {
                        Console.WriteLine("Commit Id: " + commitRef.CommitId + "\t By: " + commitRef.Author.Name + " @ " + commitRef.Author.Date);
                        foreach (ResourceRef workItemRef in commitRef.WorkItems)
                        {                            
                            WorkItem workItem = workItemTrackingHttpClient.GetWorkItemAsync(int.Parse(workItemRef.Id)).Result;
                            Console.WriteLine("\t WorkItem Id: " + workItem.Id + "\t " + workItem.Fields["System.Title"]);
                        }
                    }
                    Console.ReadLine();

                }
                else if (repo.Name == pullRequestDiffRepo)
                {
                    //场景2：通过两个TAG之间的PULL REQUEST获取相关的工作项
                    //string prBaseline001 = "refs/tags/PR-BASELINE-001";
                    string prBaseline001 = "refs/tags/PR-BASELINE-002";

                    List<GitRef> tagRefs = gitHttpClient.GetTagRefsAsync(repo.Id).Result;

                    GitAnnotatedTag annotedTagPrBaseline001 = gitHttpClient.GetAnnotatedTagAsync(
                        projectName,
                        repo.Id,
                        (tagRefs.Find(x => x.Name == prBaseline001)).ObjectId
                        ).Result;

                    Console.WriteLine("Tag: " + prBaseline001 + "\t -> Commit ID: " + annotedTagPrBaseline001.TaggedObject.ObjectId);

                    GitPullRequestQuery prQuery = new GitPullRequestQuery()
                    {
                        QueryInputs = new List<GitPullRequestQueryInput>()
                    };

                    GitPullRequestQueryInput prQueryInput = new GitPullRequestQueryInput()
                    {
                        Items = new List<string>()
                    };
                    prQueryInput.Type = GitPullRequestQueryType.LastMergeCommit;
                    prQueryInput.Items.Add(annotedTagPrBaseline001.TaggedObject.ObjectId);
                    prQuery.QueryInputs.Add(prQueryInput);

                    //GitPullRequestQueryType prQueryType = new GitPullRequestQueryType();

                    GitPullRequestQuery prQueryResult = gitHttpClient.GetPullRequestQueryAsync(prQuery, repo.Id).Result;
                    IDictionary<string, List<GitPullRequest>> findPRDirectory = prQueryResult.Results[0];
                    GitPullRequest findPR =  findPRDirectory[annotedTagPrBaseline001.TaggedObject.ObjectId].First<GitPullRequest>();

                    GitPullRequest prDetails = gitHttpClient.GetPullRequestAsync(
                        repo.Id,
                        findPR.PullRequestId,
                        null, 
                        null, 
                        null, 
                        true, //includeCommit
                        true, //includeWorkItemRefs
                        null
                        ).Result;

                    Console.WriteLine("\t Linked PR " + prDetails.PullRequestId + ": " + prDetails.Title);

                    Console.WriteLine("\t --> Related WorkItems ");
                    foreach (ResourceRef workItemRef in prDetails.WorkItemRefs)
                    {
                        WorkItem workItem = workItemTrackingHttpClient.GetWorkItemAsync(int.Parse(workItemRef.Id)).Result;
                        Console.WriteLine("\t\t WorkItem Id: " + workItem.Id + "\t " + workItem.Fields["System.Title"]);
                    }

                    Console.WriteLine("\t --> Related Commits ");
                    foreach (GitCommitRef commitRef in prDetails.Commits)
                    {
                        Console.WriteLine("\t\t Commit Id: " + commitRef.CommitId + "\t By " + commitRef.Committer.Name + " @ " + commitRef.Committer.Date);
                    }
                    Console.WriteLine("\t --> Related Reviewers ");
                    foreach (IdentityRefWithVote reviewerRef in prDetails.Reviewers)
                    {
                        Console.WriteLine("\t\t Reviewer: " + reviewerRef.DisplayName + "\t Vote Value: " + reviewerRef.Vote);
                    }

                    Console.ReadLine();
                }
            }
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
