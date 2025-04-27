import React, { createContext, useContext, useEffect, useState } from "react";
import { Job } from "../modals/Job";
import { fetchJobs } from "../services/jobApi";
import signalRService from "../services/signalRService";
import { JobEvent } from "../modals/JobEvent";
import { handleJobProgressUpdate } from "../handlers/eventHandlers";

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
      await refreshJobs();
      await signalRService.startConnection();
    };

    const unsubscribeJobProgressUpdates = signalRService.subscribeToEvent(
      JobEvent.UpdateJobProgress,
      (update) => handleJobProgressUpdate(update, setJobs)
    );

    initialize();

    return () => {
      unsubscribeJobProgressUpdates();
      signalRService.stopConnection();
    };
  }, []);

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
