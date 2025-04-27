import React, { useState } from "react";
import { useJobs } from "../../context/JobContext";
import { JobStatus } from "../../modals/Job";
import JobTable from "./JobTable";
import Modal from "../common/Modal";
import JobForm from "../JobForm/JobForm";
import { deleteJobsByStatus } from "../../services/jobApi";
import ConfirmationModal from "../common/ConfirmationModal";
import "./JobDashboard.css";

const JobDashboard: React.FC = () => {
  const { jobs, loading, error, refreshJobs } = useJobs();
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isDeleteJobsModalOpen, setIsDeleteJobsModalOpen] = useState(false);
  const [selectedJobStatus, setSelectedJobStatus] = useState<JobStatus>(
    JobStatus.Failed
  ); // Default to Failed
  const [isProcessing, setIsProcessing] = useState(false);

  const handleDeleteJobs = async () => {
    setIsProcessing(true);
    try {
      await deleteJobsByStatus(selectedJobStatus);
      await refreshJobs();
      setIsDeleteJobsModalOpen(false);
    } catch (error) {
      console.error("Failed to delete jobs:", error);
    } finally {
      setIsProcessing(false);
    }
  };

  const countJobsByStatus = (status: JobStatus) => {
    return jobs.filter((job) => job.status === status).length;
  };

  const hasCompletedJobs = countJobsByStatus(JobStatus.Completed) > 0;
  const hasFailedJobs = countJobsByStatus(JobStatus.Failed) > 0;
  const hasStoppedJobs = countJobsByStatus(JobStatus.Stopped) > 0;

  return (
    <div className="job-dashboard">
      <div className="dashboard-header">
        <h1>Job Dashboard</h1>
        <div className="dashboard-actions">
          <button
            className="btn-primary"
            onClick={() => setIsCreateModalOpen(true)}
          >
            Create New Job
          </button>

          {(hasFailedJobs || hasStoppedJobs) && (
            <button
              className="btn-secondary"
              onClick={() => setIsDeleteJobsModalOpen(true)}
            >
              Delete Jobs
            </button>
          )}
        </div>
      </div>

      <div className="dashboard-summary">
        <div className="summary-card pending">
          <div className="summary-value">
            {countJobsByStatus(JobStatus.Pending)}
          </div>
          <div className="summary-label">Pending</div>
        </div>
        <div className="summary-card running">
          <div className="summary-value">
            {countJobsByStatus(JobStatus.Running)}
          </div>
          <div className="summary-label">Running</div>
        </div>
        <div className="summary-card completed">
          <div className="summary-value">
            {countJobsByStatus(JobStatus.Completed)}
          </div>
          <div className="summary-label">Completed</div>
        </div>
        <div className="summary-card failed">
          <div className="summary-value">
            {countJobsByStatus(JobStatus.Failed)}
          </div>
          <div className="summary-label">Failed</div>
        </div>
        <div className="summary-card stopped">
          <div className="summary-value">
            {countJobsByStatus(JobStatus.Stopped)}
          </div>
          <div className="summary-label">Stopped</div>
        </div>
      </div>

      {error && (
        <div className="error-banner">
          {error}
          <button onClick={refreshJobs} className="btn-sm">
            Try Again
          </button>
        </div>
      )}

      {loading ? (
        <div className="loading-indicator">Loading jobs...</div>
      ) : (
        <JobTable jobs={jobs} />
      )}

      {/* Create Job Modal */}
      <Modal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        title="Create New Job"
      >
        <JobForm onClose={() => setIsCreateModalOpen(false)} />
      </Modal>

      {/* Delete Jobs Modal */}
      <ConfirmationModal
        isOpen={isDeleteJobsModalOpen}
        onClose={() => setIsDeleteJobsModalOpen(false)}
        onConfirm={handleDeleteJobs}
        title="Delete Jobs"
        message="Are you sure you want to delete jobs with the selected status?"
        confirmText="Delete Selected Jobs"
        isLoading={isProcessing}
      >
        {/* Radio buttons to select status */}
        <div className="delete-job-options">
          <label>
            <input
              type="radio"
              value={JobStatus.Failed}
              checked={selectedJobStatus === JobStatus.Failed}
              onChange={() => setSelectedJobStatus(JobStatus.Failed)}
            />
            Failed Jobs
          </label>
          <label>
            <input
              type="radio"
              value={JobStatus.Stopped}
              checked={selectedJobStatus === JobStatus.Stopped}
              onChange={() => setSelectedJobStatus(JobStatus.Stopped)}
            />
            Stopped Jobs
          </label>
        </div>
      </ConfirmationModal>
    </div>
  );
};

export default JobDashboard;
