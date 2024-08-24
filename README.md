# Forecastle
Forecastle is a general-purpose resource monitoring and management tool for Kubernetes clusters.  
It reads your configuration from your local kubeconfig, and shows a graphical representation of your cluster.  
It's very early in development, and as such, designs are very fragile and basic.  

### Planned features
- Show all cluster resources (by namespace) in an aesthetically pleasing graph
   - Show relationships between resources (i.e. pod -> persistent volume, ingress -> pod) on the graph
- Allow user to "open" a resource by clicking on it
   - Live following of that resources' logs and inspection status
   - Preview of resource specification
- "Dry run" preview of a spec edit
  - Show knock-on effects to other resources
- Kustomize integration?

Forecastle runs using Avalonia on all desktop platforms, and (potentially) mobile in the future.
### Example
<img width="1387" alt="image" src="https://github.com/user-attachments/assets/6f4cf028-f932-4aa8-a92e-533297286b5e">
