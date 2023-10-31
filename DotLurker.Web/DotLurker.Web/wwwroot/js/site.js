async function loadTree(){
    const msBuildPath = "C:\\Program Files\\dotnet\\sdk\\7.0.203";
    const solutionPath = "D:\\RiderProjects\\DotLurker\\DotLurker\\DotLurker.sln";
    
    return $.getJSON(`/tree?msBuildPath=${msBuildPath}&solutionPath=${solutionPath}`);
}