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
  const [isDeleteCompletedModalOpen, setIsDeleteCompletedModalOpen] =
    useState(false);
  const [isDeleteFailedModalOpen, setIsDeleteFailedModalOpen] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);

  const handleDeleteCompleted = async () => {
    setIsProcessing(true);
    try {
      await deleteJobsByStatus(JobStatus.Completed);
      await refreshJobs();
      setIsDeleteCompletedModalOpen(false);
    } catch (error) {
      console.error("Failed to delete jobs:", error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleDeleteFailed = async () => {
    setIsProcessing(true);
    try {
      await deleteJobsByStatus(JobStatus.Failed);
      await refreshJobs();
      setIsDeleteFailedModalOpen(false);
    } catch (error) {
      console.error("Failed to delete failed jobs:", error);
    } finally {
      setIsProcessing(false);
    }
  };

  const countJobsByStatus = (status: JobStatus) => {
    return jobs.filter((job) => job.status === status).length;
  };

  const hasCompletedJobs = countJobsByStatus(JobStatus.Completed) > 0;
  const hasFailedJobs = countJobsByStatus(JobStatus.Failed) > 0;

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

          {hasCompletedJobs && (
            <button
              className="btn-secondary"
              onClick={() => setIsDeleteCompletedModalOpen(true)}
            >
              Delete Jobs
            </button>
          )}

          {hasFailedJobs && (
            <button
              className="btn-danger"
              onClick={() => setIsDeleteFailedModalOpen(true)}
            >
              Delete Failed Jobs
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
        <div className="summary-card inqueue">
          <div className="summary-value">
            {countJobsByStatus(JobStatus.InQueue)}
          </div>
          <div className="summary-label">In Queue</div>
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

      {/* Delete Completed Jobs Modal */}
      <ConfirmationModal
        isOpen={isDeleteCompletedModalOpen}
        onClose={() => setIsDeleteCompletedModalOpen(false)}
        onConfirm={handleDeleteCompleted}
        title="Delete Jobs"
        message="Are you sure you want to delete all completed jobs? This action cannot be undone."
        confirmText="Delete All"
        isLoading={isProcessing}
      />

      {/* Delete Failed Jobs Modal */}
      <ConfirmationModal
        isOpen={isDeleteFailedModalOpen}
        onClose={() => setIsDeleteFailedModalOpen(false)}
        onConfirm={handleDeleteFailed}
        title="Delete Failed Jobs"
        message="Are you sure you want to delete all failed jobs? This action cannot be undone."
        confirmText="Delete All"
        isLoading={isProcessing}
      />
    </div>
  );
};

export default JobDashboard;
