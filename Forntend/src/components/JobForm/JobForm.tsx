import React, { useState } from "react";
import { createJob } from "../../services/jobApi";
import { JobPriority } from "../../modals/Job";
import { useJobs } from "../../context/JobContext";
import "./JobForm.css";

interface JobFormProps {
  onClose: () => void;
}

const JobForm: React.FC<JobFormProps> = ({ onClose }) => {
  const { refreshJobs } = useJobs();
  const [jobName, setJobName] = useState("");
  const [priority, setPriority] = useState<JobPriority>(JobPriority.Regular);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!jobName.trim()) {
      setError("Job name is required");
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      await createJob({
        name: jobName,
        priority,
      });
      await refreshJobs();
      onClose();
    } catch (err) {
      setError("Failed to create job");
      console.error(err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="job-form">
      {error && <div className="error-message">{error}</div>}

      <div className="form-group">
        <label htmlFor="jobName" className="form-label">
          Job Name
        </label>
        <input
          id="jobName"
          type="text"
          className="form-control"
          value={jobName}
          onChange={(e) => setJobName(e.target.value)}
          placeholder="Enter job name"
          disabled={isSubmitting}
          required
        />
      </div>

      <div className="form-group">
        <label className="form-label">Priority</label>
        <div className="priority-options">
          <label className="priority-option">
            <input
              type="radio"
              name="priority"
              checked={priority === JobPriority.Regular}
              onChange={() => setPriority(JobPriority.Regular)}
              disabled={isSubmitting}
            />
            <span className="priority-label regular">Regular</span>
          </label>

          <label className="priority-option">
            <input
              type="radio"
              name="priority"
              checked={priority === JobPriority.High}
              onChange={() => setPriority(JobPriority.High)}
              disabled={isSubmitting}
            />
            <span className="priority-label high">High</span>
          </label>
        </div>
      </div>

      <div className="form-actions">
        <button
          type="button"
          className="btn-secondary"
          onClick={onClose}
          disabled={isSubmitting}
        >
          Cancel
        </button>
        <button type="submit" className="btn-primary" disabled={isSubmitting}>
          {isSubmitting ? "Creating..." : "Create Job"}
        </button>
      </div>
    </form>
  );
};

export default JobForm;
