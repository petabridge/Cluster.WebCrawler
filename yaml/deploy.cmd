@echo off
REM deploys all Kubernetes services to their staging environment

set namespace=webcrawler
set location=%~dp0

echo "Deploying K8s resources from [%location%] into namespace [%namespace%]"

echo "Creating Namespaces..."
kubectl create ns ""%namespace%""

echo "Using namespace [%namespace%] going forward..."

echo "Creating services from YAML files in [%location%]"
for %%f in (%location%/*.yaml) do (
    echo "Deploying %%~nxf"
    kubectl apply -f "%location%/%%~nxf" -n "%namespace%"
)
echo "All services started... Printing K8s output.."
kubectl get all -n "%namespace%"