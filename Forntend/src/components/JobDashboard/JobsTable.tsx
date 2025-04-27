import React, { useState } from "react";
import { Job, JobStatus } from "../../modals/Job";
import {
  formatTimestamp,
  getStatusLabel,
  getStatusCssClass,
  getPriorityLabel,
  getPriorityClass,
} from "../../utils/jobFormatter";
import { stopJob, restartJob, deleteJob } from "../../services/jobApi";
import { useJobs } from "../../context/JobContext";
import ConfirmationModal from "../common/ConfirmationModal";
import "./JobsTable.css";

interface JobsTableProps {
  jobs: Job[];
}

const JobsTable: React.FC<JobsTableProps> = ({ jobs }) => {
  const { refreshJobs } = useJobs();
  const [selectedJob, setSelectedJob] = useState<Job | null>(null);
  const [modalType, setModalType] = useState<
    "delete" | "stop" | "restart" | null
  >(null);
  const [isProcessing, setIsProcessing] = useState(false);

  const handleStopJob = async () => {
    if (!selectedJob) return;

    setIsProcessing(true);
    try {
      await stopJob(selectedJob.jobID);
      await refreshJobs();
      closeModal();
    } catch (error) {
      console.error("Failed to stop job:", error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleRestartJob = async () => {
    if (!selectedJob) return;

    setIsProcessing(true);
    try {
      await restartJob(selectedJob.jobID);
      await refreshJobs();
      closeModal();
    } catch (error) {
      console.error("Failed to restart job:", error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleDeleteJob = async () => {
    if (!selectedJob) return;

    setIsProcessing(true);
    try {
      await deleteJob(selectedJob.jobID);
      await refreshJobs();
      closeModal();
    } catch (error) {
      console.error("Failed to delete job:", error);
    } finally {
      setIsProcessing(false);
    }
  };

  const openModal = (job: Job, type: "delete" | "stop" | "restart") => {
    setSelectedJob(job);
    setModalType(type);
  };

  const closeModal = () => {
    setSelectedJob(null);
    setModalType(null);
  };

  const renderModalContent = () => {
    if (!selectedJob || !modalType) return null;

    switch (modalType) {
      case "delete":
        return (
          <ConfirmationModal
            isOpen={true}
            onClose={closeModal}
            onConfirm={handleDeleteJob}
            title="Confirm Delete"
            message={`Are you sure you want to delete the job "${selectedJob.name}"?`}
            confirmText="Delete"
            isLoading={isProcessing}
          />
        );
      case "stop":
        return (
          <ConfirmationModal
            isOpen={true}
            onClose={closeModal}
            onConfirm={handleStopJob}
            title="Confirm Stop"
            message={`Are you sure you want to stop the job "${selectedJob.name}"?`}
            confirmText="Stop"
            isLoading={isProcessing}
          />
        );
      case "restart":
        return (
          <ConfirmationModal
            isOpen={true}
            onClose={closeModal}
            onConfirm={handleRestartJob}
            title="Confirm Restart"
            message={`Are you sure you want to restart the job "${selectedJob.name}"?`}
            confirmText="Restart"
            isLoading={isProcessing}
          />
        );
      default:
        return null;
    }
  };

  const isJobStoppable = (job: Job) =>
    job.status === JobStatus.Running || job.status === JobStatus.Pending;
  const isJobRestartable = (job: Job) =>
    job.status === JobStatus.Failed || job.status === JobStatus.Stopped;
  const isJobDeletable = (job: Job) =>
    job.status === JobStatus.Completed || job.status === JobStatus.Failed;

  return (
    <div className="job-table-container">
      <table className="job-table">
        <thead>
          <tr>
            <th>Job Name</th>
            <th>Priority</th>
            <th>Status</th>
            <th>Progress</th>
            <th>Start Time</th>
            <th>End Time</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {jobs.length === 0 ? (
            <tr>
              <td colSpan={7} className="no-jobs">
                No jobs available
              </td>
            </tr>
          ) : (
            jobs.map((job) => (
              <tr key={job.jobID}>
                <td>{job.name}</td>
                <td>
                  <span
                    className={`priority-badge ${getPriorityClass(
                      job.priority
                    )}`}
                  >
                    {getPriorityLabel(job.priority)}
                  </span>
                </td>
                <td>
                  <span
                    className={`status-badge ${getStatusCssClass(job.status)}`}
                  >
                    {getStatusLabel(job.status)}
                  </span>
                </td>
                <td>
                  <div className="progress-bar">
                    <div
                      className={`progress-bar-fill ${getStatusCssClass(
                        job.status
                      )}`}
                      style={{ width: `${job.progress}%` }}
                    />
                  </div>
                  <span className="progress-text">{job.progress}%</span>
                </td>
                <td>{formatTimestamp(job.startedAt)}</td>
                <td>{formatTimestamp(job.completedAt)}</td>
                <td>
                  <div className="action-buttons">
                    {isJobStoppable(job) && (
                      <button
                        className="action-button stop"
                        title="Stop Job"
                        onClick={() => openModal(job, "stop")}
                      >
                        Stop
                      </button>
                    )}
                    {isJobRestartable(job) && (
                      <button
                        className="action-button restart"
                        title="Restart Job"
                        onClick={() => openModal(job, "restart")}
                      >
                        Restart
                      </button>
                    )}
                    {isJobDeletable(job) && (
                      <button
                        className="action-button delete"
                        title="Delete Job"
                        onClick={() => openModal(job, "delete")}
                      >
                        Delete
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>

      {renderModalContent()}
    </div>
  );
};

export default JobsTable;
