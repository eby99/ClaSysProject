$(document).ready(function() {
    // Auto-hide alerts after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut();
    }, 5000);


    // Auto-focus on username field if login form is visible
    if ($('.login-form').length > 0) {
        $('input[name="Username"]').focus();
    }
});

let currentUserId = null;
let currentUsername = null;
let currentIsActive = null;

function showToggleModal(userId, username, isActive) {
    currentUserId = userId;
    currentUsername = username;
    currentIsActive = isActive;

    const action = isActive ? 'deactivate' : 'activate';
    const icon = isActive ? 'fa-toggle-off' : 'fa-toggle-on';
    const color = isActive ? '#ff6b6b' : '#10B981';

    document.getElementById('modalTitle').textContent = isActive ? 'Deactivate User' : 'Activate User';
    document.getElementById('modalMessage').textContent = `Are you sure you want to ${action} this user?`;
    document.getElementById('modalUsername').textContent = username;
    document.getElementById('modalIcon').className = `fas ${icon}`;
    document.getElementById('modalIcon').style.color = color;
    document.getElementById('confirmButton').innerHTML = `<i class="fas fa-check"></i> ${action.charAt(0).toUpperCase() + action.slice(1)}`;

    document.getElementById('confirmModal').style.display = 'flex';

    // Return false to prevent the form submission
    return false;
}

function closeModal() {
    document.getElementById('confirmModal').style.display = 'none';
    currentUserId = null;
    currentUsername = null;
    currentIsActive = null;
}

function confirmToggle() {
    if (currentUserId) {
        // Show loading state
        document.getElementById('confirmButton').innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
        document.getElementById('confirmButton').disabled = true;

        // Submit the form
        const form = document.getElementById(`toggleForm_${currentUserId}`);
        if (form) {
            // Add a hidden input to track that we're processing
            const processingInput = document.createElement('input');
            processingInput.type = 'hidden';
            processingInput.name = 'processing';
            processingInput.value = 'true';
            form.appendChild(processingInput);

            form.submit();
        } else {
            console.error('Form not found for user ID:', currentUserId);
            closeModal();
        }
    } else {
        console.error('No user ID set');
        closeModal();
    }
}

// Close modal when clicking outside of it
window.onclick = function(event) {
    const modal = document.getElementById('confirmModal');
    if (event.target == modal) {
        closeModal();
    }
}

// Approval Modal Variables
let currentApprovalUserId = null;
let currentApprovalUsername = null;
let currentApprovalAction = null;

function showApprovalModal(userId, username, action) {
    currentApprovalUserId = userId;
    currentApprovalUsername = username;
    currentApprovalAction = action;

    if (action === 'approve') {
        document.getElementById('modalTitle').textContent = 'Approve User';
        document.getElementById('modalMessage').textContent = 'Are you sure you want to approve this user? They will be able to login after approval.';
        document.getElementById('modalIcon').className = 'fas fa-check-circle';
        document.getElementById('modalIcon').style.color = '#10B981';
        document.getElementById('confirmButton').innerHTML = '<i class="fas fa-check"></i> Approve User';
        document.getElementById('confirmButton').className = 'btn btn-primary';
    } else if (action === 'reject') {
        document.getElementById('modalTitle').textContent = 'Reject User';
        document.getElementById('modalMessage').textContent = 'Are you sure you want to reject and delete this user? This action cannot be undone.';
        document.getElementById('modalIcon').className = 'fas fa-times-circle';
        document.getElementById('modalIcon').style.color = '#ef4444';
        document.getElementById('confirmButton').innerHTML = '<i class="fas fa-times"></i> Reject User';
        document.getElementById('confirmButton').className = 'btn btn-danger';
    }

    document.getElementById('modalUsername').textContent = username;
    document.getElementById('confirmModal').style.display = 'flex';

    // Update the confirm button to use the new action
    document.getElementById('confirmButton').onclick = confirmApproval;

    return false;
}

function confirmApproval() {
    if (currentApprovalUserId && currentApprovalAction) {
        // Show loading state
        document.getElementById('confirmButton').innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
        document.getElementById('confirmButton').disabled = true;

        // Submit the appropriate form
        const formId = currentApprovalAction === 'approve' ? `approveForm_${currentApprovalUserId}` : `rejectForm_${currentApprovalUserId}`;
        const form = document.getElementById(formId);

        if (form) {
            // Add a hidden input to track that we're processing
            const processingInput = document.createElement('input');
            processingInput.type = 'hidden';
            processingInput.name = 'processing';
            processingInput.value = 'true';
            form.appendChild(processingInput);

            form.submit();
        } else {
            console.error('Form not found for user ID:', currentApprovalUserId);
            closeApprovalModal();
        }
    } else {
        console.error('No user ID or action set');
        closeApprovalModal();
    }
}

function closeApprovalModal() {
    document.getElementById('confirmModal').style.display = 'none';
    currentApprovalUserId = null;
    currentApprovalUsername = null;
    currentApprovalAction = null;

    // Reset the confirm button onclick to the original toggle function
    document.getElementById('confirmButton').onclick = confirmToggle;
    document.getElementById('confirmButton').disabled = false;
}

// Delete User Modal Variables
let currentDeleteUserId = null;
let currentDeleteUsername = null;

function showDeleteModal(userId, username) {
    currentDeleteUserId = userId;
    currentDeleteUsername = username;

    document.getElementById('modalTitle').textContent = 'Delete User Account';
    document.getElementById('modalMessage').textContent = 'Are you sure you want to permanently delete this user account? This action cannot be undone and will remove all user data.';
    document.getElementById('modalIcon').className = 'fas fa-trash';
    document.getElementById('modalIcon').style.color = '#ef4444';
    document.getElementById('modalUsername').textContent = username;
    document.getElementById('confirmButton').innerHTML = '<i class="fas fa-trash"></i> Delete Permanently';
    document.getElementById('confirmButton').className = 'btn btn-danger';
    document.getElementById('confirmModal').style.display = 'flex';

    // Update the confirm button to use the delete action
    document.getElementById('confirmButton').onclick = confirmDelete;

    return false;
}

function confirmDelete() {
    if (currentDeleteUserId) {
        // Show loading state
        document.getElementById('confirmButton').innerHTML = '<i class="fas fa-spinner fa-spin"></i> Deleting...';
        document.getElementById('confirmButton').disabled = true;

        // Submit the delete form
        const form = document.getElementById(`deleteForm_${currentDeleteUserId}`);
        if (form) {
            // Add a hidden input to track that we're processing
            const processingInput = document.createElement('input');
            processingInput.type = 'hidden';
            processingInput.name = 'processing';
            processingInput.value = 'true';
            form.appendChild(processingInput);

            form.submit();
        } else {
            console.error('Delete form not found for user ID:', currentDeleteUserId);
            closeDeleteModal();
        }
    } else {
        console.error('No user ID set for deletion');
        closeDeleteModal();
    }
}

function closeDeleteModal() {
    document.getElementById('confirmModal').style.display = 'none';
    currentDeleteUserId = null;
    currentDeleteUsername = null;

    // Reset the confirm button onclick to the original toggle function
    document.getElementById('confirmButton').onclick = confirmToggle;
    document.getElementById('confirmButton').disabled = false;
}