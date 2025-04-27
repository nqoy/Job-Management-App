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
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refreshJobs = async () => {
    setLoading(true);
    try {
      const jobsData = await fetchJobs();
      setJobs(jobsData);
      setError(null);
    } catch (err) {
      setError("Failed to fetch jobs");
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

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
