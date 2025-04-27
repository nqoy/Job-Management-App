import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from "react";
import { Job } from "../modals/Job";
import { fetchJobs } from "../services/jobApi";
import { JobEvent } from "../modals/JobEvent";
import { handleJobProgressUpdate } from "../handlers/eventHandlers";
import { JobProgressUpdate } from "../modals/Job";
import useSignalRSubscription from "../hooks/useSignalREventSub";
import signalRService from "../services/signalRService";
import useLocalStorage from "../hooks/useLocalStorage";

interface JobContextType {
  jobs: Job[];
  loading: boolean;
  error: string | null;
  refreshJobs: () => Promise<void>;
}

const JobContext = createContext<JobContextType | undefined>(undefined);

export const JobProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [jobs, setJobs] = useLocalStorage<Job[]>("jobs", []);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refreshJobs = async () => {
    setLoading(true);
    try {
      const jobsData = await fetchJobs();
      const mergedJobs = mergeJobs(jobsData, jobs);

      setJobs(mergedJobs);
      setError(null);
    } catch (err) {
      setError("Failed to fetch jobs");
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  function mergeJobs(fetchedJobs: Job[], localJobs: Job[]): Job[] {
    if (fetchedJobs.length === 0) return [];
    if (localJobs.length === 0) return fetchedJobs;

    const localJobsMap = new Map(localJobs.map((job) => [job.jobID, job]));

    return fetchedJobs.map((job) => {
      const localJob = localJobsMap.get(job.jobID);
      if (localJob) {
        return { ...job, progress: localJob.progress };
      }
      return job;
    });
  }

  useEffect(() => {
    const initialize = async () => {
      await signalRService.startConnection();
      await refreshJobs();
    };
    initialize();
  }, []);

  const handleJobProgressUpdateMemoized = useCallback(
    (update: JobProgressUpdate) => {
      handleJobProgressUpdate(update, setJobs);
    },
    []
  );

  useSignalRSubscription<JobProgressUpdate>(
    JobEvent.UpdateJobProgress,
    handleJobProgressUpdateMemoized
  );

  return (
    <JobContext.Provider value={{ jobs, loading, error, refreshJobs }}>
      {children}
    </JobContext.Provider>
  );
};

export const useJobs = () => {
  const context = useContext(JobContext);
  if (context === undefined) {
    throw new Error("useJobs must be used within a JobProvider");
  }
  return context;
};
