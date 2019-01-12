pipeline {
    agent {
        node {
            label 'master'
            customWorkspace 'workspace/Dangl.EvaluationPackageGenerator'
        }
    }
    environment {
        KeyVaultBaseUrl = credentials('AzureCiKeyVaultBaseUrl')
        KeyVaultClientId = credentials('AzureCiKeyVaultClientId')
        KeyVaultClientSecret = credentials('AzureCiKeyVaultClientSecret')        
    }
    stages {
        stage ('Docs & CLI Tool Publish') {
            steps {
                powershell './build.ps1 UploadDocumentation'
            }
        }
    }
    post {
        always {
            step([$class: 'Mailer',
                notifyEveryUnstableBuild: true,
                recipients: "georg@dangl.me",
                sendToIndividuals: true])
        }
    }
}