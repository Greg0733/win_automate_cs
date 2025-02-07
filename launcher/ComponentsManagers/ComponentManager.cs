using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace launcher.ComponentsManagers
{
	public abstract class ComponentManager
    {
        internal static readonly HttpClient httpClientInstance = new();

        internal const int WIN32_FILE_NOT_FOUND = 0x2;
        internal const int WIN32_PATH_NOT_FOUND = 0x3;

        internal static readonly string appDir = AppDomain.CurrentDomain.BaseDirectory;
        internal static readonly string toolsDir = Path.Join(appDir, "Tools");
        internal static readonly string? userDir = Environment.GetEnvironmentVariable("UserProfile");
        internal static readonly string? userShortcutsDir = Path.Join(Environment.GetEnvironmentVariable("AppData"), "Microsoft", "Windows", "Start Menu", "Programs", "win_automate");
        internal static readonly string? programFilesDir = Environment.GetEnvironmentVariable("ProgramFiles");
        internal static readonly string tempDir = Path.GetTempPath();

        protected static readonly ComponentManager[] emptyPrerequisites = [];

        private double currInstallCoefficient;
        private int currSubStep;
        private readonly HashSet<ComponentManager> missingPrerequisites = [];
        private bool? isInstalledField = null;
        private IProgress<ProgressData>? subStepProgress;

        internal abstract bool CheckInstalled(IProgress<string>? logsProgress = null);
        internal abstract void StartConfiguration();
        internal abstract void StartComponent();

        internal void InstallAsync(IProgress<ProgressData> installProgress)
        {
            Task.Run(async () =>
            {
                try
                {
                    // Compute all prerequisites
                    HashSet<ComponentManager> toInstall =
					[
						this
                    ];
                    List<ComponentManager> prerequisitesCheck =
					[
						this
                    ];
                    for (int i = 0; i < prerequisitesCheck.Count; i++)
                    {
                        ComponentManager potentialInstall = prerequisitesCheck[i];
                        AddMissingPrerequisites(potentialInstall.Corequisites, toInstall, prerequisitesCheck);
                        AddMissingPrerequisites(potentialInstall.Prerequisites, toInstall, prerequisitesCheck);
                    }

                    // Remove prerequisites already installed
                    HashSet<Task> checkTasks = [];
                    // TODO a SynchronizationContext may be needed to update the global progress
                    IProgress<string> storeInstalledProgress = new Progress<string>(s =>
                    {
                        installProgress.Report(new(0, s));
                    });
                    foreach (ComponentManager componentManager in toInstall)
                    {
                        // TODO Task.Run is maybe required, async keyword not always working as expected
                        checkTasks.Add(componentManager.StoreInstalled(storeInstalledProgress));
                    }
                    await Task.WhenAll(checkTasks);

                    toInstall.RemoveWhere((componentManager) => componentManager.IsInstalled);

                    // Setup missing prerequisites
                    foreach (ComponentManager componentManager in toInstall)
                    {
                        foreach (ComponentManager installPrerequisite in componentManager.Prerequisites)
                        {
                            if (!installPrerequisite.IsInstalled)
                            {
                                componentManager.missingPrerequisites.Add(installPrerequisite);
                            }
                        }
                    }

                    // Compute total weight and coefficients for each installer
                    // progress = sum_{installers} currSubStep / NbSubSteps * Weight / totWeight
                    // This grows linearly in each currSubStep value, from 0 to 1
                    double totWeight = 0;
                    foreach (ComponentManager componentManager in toInstall)
                    {
                        totWeight += componentManager.Weight;
                    }
                    foreach (ComponentManager componentManager in toInstall)
                    {
                        componentManager.currInstallCoefficient = componentManager.Weight / (totWeight * componentManager.NbSubSteps);
                        componentManager.currSubStep = 0;
                    }

                    double totalProgress = 0;
                    // Run all installers in parallel, receive progress updates from each one, and update global progress accordingly
                    // TODO a SynchronizationContext may be needed to update the global progress
                    IProgress<ProgressData> subStepProgress = new Progress<ProgressData>(data =>
                    {
                        if (data.progress != null)
                            totalProgress += (double)data.progress;

                        installProgress.Report(new(totalProgress, data.message, data.exception));
                    });

                    HashSet<Task<ComponentManager>> installTasks = [];
                    Dictionary<ComponentManager, HashSet<ComponentManager>> completedToNexts = [];
                    foreach (ComponentManager componentManager in toInstall)
                    {
                        // TODO Task.Run is maybe required, async keyword not always working as expected
                        if (componentManager.missingPrerequisites.Count == 0)
                        {
                            installTasks.Add(componentManager.StartInstall(subStepProgress));
                        }
                        else
                        {
                            AddMissings(componentManager, completedToNexts);
                        }
                    }

                    await RunInOrder(installTasks, completedToNexts, subStepProgress);

                    installProgress.Report(new(finished: true));
                } catch (Exception e)
                {
                    installProgress.Report(new(message: e.Message, exception: true, finished: true));
                }
            });
        }

        protected void ReportInstallProgress(string? message = null, bool installDone = false)
        {
            if (subStepProgress == null) throw new InvalidOperationException($"{nameof(subStepProgress)} should be set before calling this function");

            int nbSubStepsDone;
            if (installDone)
            {
                nbSubStepsDone = NbSubSteps - currSubStep;
            }
            else
            {
                nbSubStepsDone = 1;
                currSubStep += 1;
            }

            subStepProgress.Report(new(nbSubStepsDone * currInstallCoefficient, message, finished: installDone));
        }

        protected abstract ComponentManager[] Corequisites
        {
            get;
        }

        protected abstract ComponentManager[] Prerequisites
        {
            get;
        }
        protected abstract int NbSubSteps
        {
            get;
        }
        protected abstract double Weight
        {
            get;
        }

        private bool IsInstalled
        {
            get
            {
                if (isInstalledField == null) throw new InvalidOperationException($"{nameof(IsInstalled)} should only be used after {nameof(StoreInstalled)} has been run");
                return (bool)isInstalledField;
            }
        }

        protected abstract Task ComponentInstallAsync();

        private async Task StoreInstalled(IProgress<string>? logsProgress)
        {
            isInstalledField = await Task.Run(() => CheckInstalled(logsProgress));
        }

        private async Task<ComponentManager> StartInstall(IProgress<ProgressData> subStepProgress)
        {
            this.subStepProgress = subStepProgress;

            try
            {
                await ComponentInstallAsync();
            } catch (Exception e)
            {
                throw new AggregateException($"Installation of {GetType().Name} failed", e);
            }
            return this;
        }

        private static void AddMissings(ComponentManager componentManager, Dictionary<ComponentManager, HashSet<ComponentManager>> dict)
        {
            foreach (ComponentManager missingPrerequisite in componentManager.missingPrerequisites)
            {
                Utils.DictOfSetsAdd(dict, missingPrerequisite, componentManager);
            }
        }

        private static void AddMissingPrerequisites<T>(T[] potentialPrerequisites, HashSet<T> alreadyAdded, List<T> toCheck)
        {
            foreach (T prerequisite in potentialPrerequisites)
            {
                if (!alreadyAdded.Contains(prerequisite))
                {
                    toCheck.Add(prerequisite);
                    alreadyAdded.Add(prerequisite);
                }
            }
        }

        private static void StartNexts(HashSet<Task<ComponentManager>> installTasks, Dictionary<ComponentManager, HashSet<ComponentManager>> completedToNexts, IProgress<ProgressData> subStepProgress,
            ComponentManager doneManager)
        {
            if (completedToNexts.TryGetValue(doneManager, out HashSet<ComponentManager>? value))
            {
                HashSet<ComponentManager> nextInstalls = value;
                foreach (ComponentManager maybeReady in nextInstalls)
                {
                    maybeReady.missingPrerequisites.Remove(doneManager);
                    if (maybeReady.missingPrerequisites.Count == 0)
                    {
                        installTasks.Add(maybeReady.StartInstall(subStepProgress));
                        nextInstalls.Remove(maybeReady);
                    }
                }

                if (nextInstalls.Count == 0)
                {
                    completedToNexts.Remove(doneManager);
                }
            }
        }

        private static async Task RunInOrder(HashSet<Task<ComponentManager>> installTasks, Dictionary<ComponentManager, HashSet<ComponentManager>> completedToNexts, IProgress<ProgressData> subStepProgress)
        {
            while (installTasks.Count > 0)
            {
                Task<ComponentManager> completedInstall = await Task.WhenAny(installTasks);
                installTasks.Remove(completedInstall);

                if (completedInstall.IsCompletedSuccessfully)
                {
                    StartNexts(installTasks, completedToNexts, subStepProgress, completedInstall.Result);
                } else
                {
                    string message;
                    if (completedInstall.Exception != null)
                        message = completedInstall.Exception.Message;
                    else
                        message = "A sub task failed";

                    subStepProgress.Report(new(message: message, exception: true));
                }
            }
        }
    }
}
