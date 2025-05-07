// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Script de diagnostic pour les problèmes de portes
document.addEventListener('DOMContentLoaded', function () {
    // Ajouter un bouton de diagnostic dans l'UI
    const headerDiv = document.querySelector('.card-header');
    if (headerDiv) {
        const diagButton = document.createElement('button');
        diagButton.className = 'btn btn-warning btn-sm ms-2';
        diagButton.innerHTML = '<i class="bi bi-bug"></i> Diagnostics';
        diagButton.onclick = runDiagnostics;

        // Ajouter après le bouton Rafraîchir
        const refreshButton = document.getElementById('refreshButton');
        if (refreshButton) {
            refreshButton.parentNode.insertBefore(diagButton, refreshButton.nextSibling);
        } else {
            headerDiv.appendChild(diagButton);
        }
    }
});

// Fonction pour exécuter les diagnostics
async function runDiagnostics() {
    showDiagnosticsModal("Exécution des diagnostics...");

    try {
        // Vérifier l'état de santé général
        const healthResult = await checkEndpoint('/api/health/check');

        // Vérifier l'état du service
        const serviceResult = await checkEndpoint('/api/doorapi/status');

        // Vérifier les appareils disponibles
        const devicesResult = await checkEndpoint('/api/doorapi/devices');

        // Vérifier les points d'accès en base de données
        const pointsAccesResult = await checkEndpoint('/api/doorapi/db/points-acces');

        // Afficher les résultats
        updateDiagnosticsModal("Résultats des diagnostics", `
            <div class="alert alert-${healthResult.success ? 'success' : 'danger'}">
                <strong>État de santé:</strong> ${healthResult.success ? 'OK' : 'Échec'}
                ${healthResult.message ? `<br><small>${healthResult.message}</small>` : ''}
            </div>
            
            <div class="alert alert-${serviceResult.success ? 'success' : 'danger'}">
                <strong>État du service:</strong> ${serviceResult.success ? 'OK' : 'Échec'}
                ${serviceResult.message ? `<br><small>${serviceResult.message}</small>` : ''}
            </div>
            
            <div class="alert alert-${devicesResult.success ? 'success' : 'danger'}">
                <strong>Appareils:</strong> ${devicesResult.success ? `${JSON.stringify(devicesResult.data)}` : 'Échec'}
                ${devicesResult.message ? `<br><small>${devicesResult.message}</small>` : ''}
            </div>
            
            <div class="alert alert-${pointsAccesResult.success ? 'success' : 'danger'}">
                <strong>Points d'accès en base:</strong> ${pointsAccesResult.success ? `Trouvés: ${pointsAccesResult.data?.length || 0}` : 'Échec'}
                ${pointsAccesResult.message ? `<br><small>${pointsAccesResult.message}</small>` : ''}
            </div>
            
            <h5>Recommandations:</h5>
            <ul>
                ${!healthResult.success ? '<li>Vérifiez que le service gRPC est en cours d\'exécution et accessible.</li>' : ''}
                ${!serviceResult.success || !serviceResult.data?.grpcConnected ? '<li>Le service gRPC n\'est pas connecté. Vérifiez la connexion réseau et les paramètres du service.</li>' : ''}
                ${!serviceResult.success || !serviceResult.data?.dbConnected ? '<li>La base de données n\'est pas connectée. Vérifiez la chaîne de connexion.</li>' : ''}
                ${devicesResult.success && devicesResult.data?.length === 0 ? '<li>Aucun appareil n\'est connecté. Vérifiez la connexion des appareils au service.</li>' : ''}
                ${pointsAccesResult.success && pointsAccesResult.data?.length === 0 ? '<li>Aucun point d\'accès n\'est enregistré dans la base de données.</li>' : ''}
            </ul>
        `);
    } catch (error) {
        updateDiagnosticsModal("Erreur de diagnostic", `
            <div class="alert alert-danger">
                Une erreur s'est produite pendant les diagnostics: ${error.message}
            </div>
            <p>Cela peut indiquer un problème avec le serveur ou les services API.</p>
        `);
    }
}

// Fonction pour vérifier un endpoint API
async function checkEndpoint(url) {
    try {
        const response = await fetch(url);

        if (!response.ok) {
            return {
                success: false,
                message: `Statut HTTP: ${response.status} ${response.statusText}`
            };
        }

        const data = await response.json();
        return {
            success: true,
            data: data,
            message: null
        };
    } catch (error) {
        return {
            success: false,
            message: error.message
        };
    }
}

// Fonction pour afficher une modale de diagnostic
function showDiagnosticsModal(message) {
    // Supprimer toute modale existante
    const existingModal = document.getElementById('diagnosticsModal');
    if (existingModal) {
        existingModal.remove();
    }

    // Créer la structure de la modale
    const modalHtml = `
        <div class="modal fade" id="diagnosticsModal" tabindex="-1" aria-labelledby="diagnosticsModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header bg-primary text-white">
                        <h5 class="modal-title" id="diagnosticsModalLabel">Diagnostics du système de portes</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body" id="diagnosticsModalBody">
                        <div class="d-flex justify-content-center">
                            <div class="spinner-border text-primary" role="status">
                                <span class="visually-hidden">Chargement...</span>
                            </div>
                            <p class="ms-3">${message}</p>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Fermer</button>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Ajouter la modale au document
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    // Afficher la modale
    const modal = new bootstrap.Modal(document.getElementById('diagnosticsModal'));
    modal.show();
}

// Fonction pour mettre à jour le contenu de la modale
function updateDiagnosticsModal(title, content) {
    const modalTitle = document.getElementById('diagnosticsModalLabel');
    const modalBody = document.getElementById('diagnosticsModalBody');

    if (modalTitle && modalBody) {
        modalTitle.textContent = title;
        modalBody.innerHTML = content;
    }
}