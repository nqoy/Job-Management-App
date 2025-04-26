import React from "react";
import Modal from "./Modal";
import "./ConfirmationModal.css";

interface ConfirmationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  isLoading?: boolean;
}

const ConfirmationModal: React.FC<ConfirmationModalProps> = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = "Confirm",
  cancelText = "Cancel",
  isLoading = false,
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title={title}>
      <div className="confirmation-modal">
        <p className="confirmation-message">{message}</p>

        <div className="confirmation-actions">
          <button
            className="btn-secondary"
            onClick={onClose}
            disabled={isLoading}
          >
            {cancelText}
          </button>

          <button
            className="btn-danger"
            onClick={onConfirm}
            disabled={isLoading}
          >
            {isLoading ? "Processing..." : confirmText}
          </button>
        </div>
      </div>
    </Modal>
  );
};

export default ConfirmationModal;
