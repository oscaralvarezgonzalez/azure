using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureBatch
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                CreateTasksAsync().Wait();
            }
            catch (AggregateException aggregateException)
            {

                foreach (var exception in aggregateException.InnerExceptions)
                {
                    Console.WriteLine(exception.ToString());
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Press return to exit...");
            Console.ReadLine();
        }

        private static async Task CreateTasksAsync()
        {

            //1. Connect with you Azure Batch account
            BatchSharedKeyCredentials credentials = new BatchSharedKeyCredentials(
                                                        Settings.Default.BatchURL,
                                                        Settings.Default.AccountName,
                                                        Settings.Default.AccountKey);


            using (var batchClient = await BatchClient.OpenAsync(credentials))
            {
                //2. Add a retry policy
                batchClient.CustomBehaviors.Add(RetryPolicyProvider.LinearRetryProvider(TimeSpan.FromSeconds(10), 3));

                //3. Create a job id
                var jobId = string.Format("MyTasks-{0}", DateTime.Now.ToString("yyyyMMdd-HHmmss"));

                try
                {
                    //4. Submit the job
                    await SubmitJobAsync(batchClient, jobId);

                    //5. Wait for the job to complete
                    await WaitForJobAndPrintOutputAsync(batchClient, jobId);
                }
                finally
                {
                    Console.WriteLine("Press enter to delete the job");
                    Console.ReadLine();

                    if (!string.IsNullOrEmpty(jobId))
                    {
                        Console.WriteLine("Deleting job: {0}", jobId);
                        batchClient.JobOperations.DeleteJob(jobId);
                    }
                }
            }
        }

        private static async Task SubmitJobAsync(BatchClient batchClient, string jobId)
        {
            Console.WriteLine("Creating the job {0}", jobId);

            //1. Create a job
            CloudJob job = batchClient.JobOperations.CreateJob();
            job.Id = jobId;

            //2. Define a start tasks for the nodes

            //Start task: Installing Chocolatey - A Machine Package Manager
            var startTask = new StartTask()
            {
                ResourceFiles = new List<ResourceFile>() {
                    new ResourceFile("https://YOUR_STORAGE_ACCOUNT.blob.core.windows.net/resources/install-choco.bat", "install-choco.bat")
                },
                CommandLine = "install-choco.bat",
                WaitForSuccess = true, //Specifies if other tasks can be scheduled on a VM which has not run the start task
                RunElevated = true
            };

            //3. For this job, ask the Azure Batch service to automatically create a pool of VMs when the job is submitted
            job.PoolInformation = new PoolInformation
            {
                AutoPoolSpecification = new AutoPoolSpecification
                {
                    AutoPoolIdPrefix = "returngis",
                    PoolSpecification = new PoolSpecification
                    {
                        TargetDedicated = 3,
                        OSFamily = "4",
                        VirtualMachineSize = "small",
                        StartTask = startTask
                    },

                    KeepAlive = false,
                    PoolLifetimeOption = PoolLifetimeOption.Job
                }
            };

            //4. Commit job to create it in the service
            await job.CommitAsync();

            //5. Define my tasks
            var commandLine = "cmd /c echo Hello world from the Batch Hello world sample!";
            Console.WriteLine("Task #1 command line: {0}", commandLine);
            await batchClient.JobOperations.AddTaskAsync(jobId, new CloudTask("taskHelloWorld", commandLine));

            var commandChoco = "cmd /c choco list --local-only";
            Console.WriteLine("Task # 2 command line: {0}", commandChoco);
            await batchClient.JobOperations.AddTaskAsync(jobId, new CloudTask("taskchoco", commandChoco));

            var echoJavaHome = "cmd /c echo %java_home%";
            Console.WriteLine("Task # 3 command line: {0}", echoJavaHome);
            await batchClient.JobOperations.AddTaskAsync(jobId, new CloudTask("taskecho", echoJavaHome));

            var jarTask = new CloudTask("jartask", "cmd /c java -jar AzureStorage-0.0.1.jar");
            Console.WriteLine("Task # 4 command line: {0}", "java -jar AzureStorage-0.0.1.jar");
            jarTask.ResourceFiles = new List<ResourceFile>();
            jarTask.ResourceFiles.Add(new ResourceFile("https://YOUR_STORAGE_ACCOUNT.blob.core.windows.net/resources/AzureStorage-0.0.1.jar", "AzureStorage-0.0.1.jar"));
            await batchClient.JobOperations.AddTaskAsync(jobId, jarTask);

        }


        private static async Task WaitForJobAndPrintOutputAsync(BatchClient batchClient, string jobId)
        {
            Console.WriteLine("Waiting for all tasks to complete on job: {0} ...", jobId);

            //1. Use a task state monitor to monitor the status of your tasks
            var taskStateMonitor = batchClient.Utilities.CreateTaskStateMonitor();

            List<CloudTask> myTasks = await batchClient.JobOperations.ListTasks(jobId).ToListAsync();

            //2. Wait for all tasks to reach the completed state.
            bool timedOut = await taskStateMonitor.WhenAllAsync(myTasks, TaskState.Completed, TimeSpan.FromMinutes(15));

            if (timedOut)
            {
                throw new TimeoutException("Timed out waiting for tasks.");
            }

            //3. Dump task output
            foreach (var task in myTasks)
            {
                Console.WriteLine("Task {0}", task.Id);

                //4. Read the standard out of the task
                NodeFile standardOutFile = await task.GetNodeFileAsync(Constants.StandardOutFileName);
                var standardOutText = await standardOutFile.ReadAsStringAsync();
                Console.WriteLine("Standard out: ");
                Console.WriteLine(standardOutText);

                Console.WriteLine();
            }
        }
    }
}
