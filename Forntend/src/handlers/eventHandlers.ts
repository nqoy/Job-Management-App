import { JobProgressUpdate } from "../modals/Job";
import { Job } from "../modals/Job";

export const handleJobProgressUpdate = (
  update: JobProgressUpdate,
  setJobs: React.Dispatch<React.SetStateAction<Job[]>>
) => {
  setJobs((prevJobs) =>
    prevJobs.map((job) =>
      job.jobID === update.jobID
        ? { ...job, status: update.status, progress: update.progress }
        : job
    )
  );
};
