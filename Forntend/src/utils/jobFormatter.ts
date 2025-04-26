import { JobPriority, JobStatus } from "../modals/Job";

export const formatTimestamp = (timestamp: number | null): string => {
  if (!timestamp) return "-";

  const date = new Date(timestamp * 1000);
  return date.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
};

export const getStatusLabel = (status: JobStatus): string => {
  switch (status) {
    case JobStatus.Pending:
      return "Pending";
    case JobStatus.Running:
      return "Running";
    case JobStatus.Completed:
      return "Completed";
    case JobStatus.Failed:
      return "Failed";
    case JobStatus.Stopped:
      return "Stopped";
    case JobStatus.InQueue:
      return "In Queue";
    default:
      return "Unknown";
  }
};

export const getStatusClass = (status: JobStatus): string => {
  switch (status) {
    case JobStatus.Pending:
      return "status-pending";
    case JobStatus.Running:
      return "status-running";
    case JobStatus.Completed:
      return "status-completed";
    case JobStatus.Failed:
      return "status-failed";
    case JobStatus.Stopped:
      return "status-stopped";
    case JobStatus.InQueue:
      return "status-inqueue";
    default:
      return "";
  }
};

export const getPriorityLabel = (priority: JobPriority): string => {
  switch (priority) {
    case JobPriority.Regular:
      return "Regular";
    case JobPriority.High:
      return "High";
    default:
      return "Unknown";
  }
};

export const getPriorityClass = (priority: JobPriority): string => {
  switch (priority) {
    case JobPriority.Regular:
      return "priority-regular";
    case JobPriority.High:
      return "priority-high";
    default:
      return "";
  }
};
