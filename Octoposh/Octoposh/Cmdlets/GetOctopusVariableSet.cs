﻿using Octoposh.Model;
using Octopus.Client.Model;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Octoposh.Cmdlets
{
    /// <summary>
    /// <para type="synopsis"> Gets Octopus Variable sets. These can belong to a specific Project or to a Library Variable set. "Variable set" is the name of the object that holds the collection of variables for both Projects and Library Sets.</para>
    /// </summary>
    /// <summary>
    /// <para type="synopsis"> Gets Octopus Variable sets. These can belong to a specific Project or to a Library Variable set. "Variable set" is the name of the object that holds the collection of variables for both Projects and Library Sets.</para>
    /// </summary>
    /// <example>   
    ///   <code>PS C:\> Get-OctopusVariableSet</code>
    ///   <para>Gets all the Project and Library variable sets of the instance</para>    
    /// </example>
    /// <example>   
    ///   <code>PS C:\> Get-OctopusVariableSet -LibrarySetName "Stands_SC"</code>
    ///   <para>Gets the Variable Set of the Library Variable Set with the name "Stands_SC"</para>    
    /// </example>
    /// <example>   
    ///   <code>PS C:\> Get-OctopusVariableSet -LibrarySetName "Stands_SC","Stands_DII"</code>
    ///   <para>Gets the LibraryVariableSets with the names "Stands_SC" and "Stands_DII"</para>
    /// </example>
    /// <example>   
    ///   <code>PS C:\> Get-OctopusVariableSet -LibrarySetName "Stands_*"</code>
    ///   <para>Gets all the LibraryVariableSets whose name matches the pattern "Stands_*"</para>    
    /// </example>
    /// <example>   
    ///   <code>PS C:\> Get-OctopusVariableSet -LibrarySetName "Stands_*" -IncludeLibrarySetUsage </code>
    ///   <para>Gets all the LibraryVariableSets whose name matches the pattern "Stands_*". Each result will also include a list of Projects on which they are being used</para>
    /// </example>
    /// <example>   
    ///   <code>PS C:\> Get-OctopusVariableSet -ProjectName "Website_Stardust","Website_Diamond"</code>
    ///   <para>Gets the Variable Sets of the Projects "Website_Stardust" and "Website_Diamond"</para>
    /// </example>
    /// <example>   
    ///   <code>PS C:\> Get-OctopusVariableSet -ProjectName "Website_Stardust" -LibrarySetName "Stands_SC"</code>
    ///   <para>Gets the Variable Sets of the Project "Website_Stardust" and the Library variable set "Stands_SC"</para>
    /// </example>
    /// <para type="link" uri="http://Octoposh.net">WebSite: </para>
    /// <para type="link" uri="https://github.com/Dalmirog/OctoPosh/">Github Project: </para>
    /// <para type="link" uri="https://github.com/Dalmirog/OctoPosh/wiki">Wiki: </para>
    /// <para type="link" uri="https://gitter.im/Dalmirog/OctoPosh#initial">QA and Feature requests: </para>
    [Cmdlet("Get", "OctopusVariableSet")]
    [OutputType(typeof(List<OutputOctopusVariableSet>))]
    [OutputType(typeof(List<VariableSetResource>))]
    public class GetOctopusVariableSet : PSCmdlet
    {
        /// <summary>
        /// <para type="description">Library Set name</para>
        /// </summary>
        [ValidateNotNullOrEmpty()]
        [Parameter(Position = 1, ValueFromPipeline = true)]
        public List<string> LibrarySetName { get; set; }

        /// <summary>
        /// <para type="description">Project name</para>
        /// </summary>
        [ValidateNotNullOrEmpty()]
        [Parameter(Position = 1, ValueFromPipeline = true)]
        public List<string> ProjectName { get; set; }

        /// <summary>
        /// <para type="description">If set to TRUE the list of Projects on which each Library Variable Set is being used will be displayer</para>
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeLibrarySetUsage { get; set; }

        /// <summary>
        /// <para type="description">If set to TRUE the cmdlet will return the basic Octopur resource. If not set or set to FALSE, the cmdlet will return a human friendly Octoposh output object</para>
        /// </summary>
        [Parameter]
        public SwitchParameter ResourceOnly { get; set; }

        private OctopusConnection _connection;

        protected override void BeginProcessing()
        {
            _connection = new NewOctopusConnection().Invoke<OctopusConnection>().ToList()[0];
            LibrarySetName = LibrarySetName?.ConvertAll(s => s.ToLower());
            ProjectName = ProjectName?.ConvertAll(s => s.ToLower());
        }

        protected override void ProcessRecord()
        {
            var baseResourceList = new List<VariableSetResource>();
            var variableSetIDs = new List<string>();

            var projectList = new List<ProjectResource>();
            var libraryVariableSetList = new List<LibraryVariableSetResource>();

            //If no Project and Library set is declared, return all the things.
            if (ProjectName == null && LibrarySetName == null)
            {
                projectList.AddRange(_connection.Repository.Projects.FindAll());
                libraryVariableSetList.AddRange(_connection.Repository.LibraryVariableSets.FindAll());
            }

            //If at least 1 project or Library variable set is defined, then just return that list instead of everything.
            else
            {
                #region Getting projects
                //Getting variable set ids from projects
                
                if(ProjectName != null) { 
                    //Multiple values but one of them is wildcarded, which is not an accepted scenario (I.e -MachineName WebServer*, Database1)
                    if (ProjectName.Any(item => WildcardPattern.ContainsWildcardCharacters(item) && ProjectName.Count > 1))
                    {
                        throw OctoposhExceptions.ParameterCollectionHasRegularAndWildcardItem("ProjectName");
                    }
                    //Only 1 wildcarded value (ie -MachineName WebServer*)
                    else if (ProjectName.Any(item => WildcardPattern.ContainsWildcardCharacters(item) && ProjectName.Count == 1))
                    {
                        var pattern = new WildcardPattern(ProjectName.First());
                        projectList.AddRange(_connection.Repository.Projects.FindMany(t => pattern.IsMatch(t.Name.ToLower())));
                    }
                    //multiple non-wildcared values (i.e. -MachineName WebServer1,Database1)
                    else if (!ProjectName.Any(WildcardPattern.ContainsWildcardCharacters))
                    {
                        projectList.AddRange(_connection.Repository.Projects.FindMany(t => ProjectName.Contains(t.Name.ToLower())));
                    }

                    variableSetIDs.AddRange(projectList.Select(p => p.VariableSetId));
                }
                #endregion

                #region Getting Library variable sets
                if (LibrarySetName != null)
                {
                    

                    //Getting variable set ids from LibraryVariableSets

                    //Multiple values but one of them is wildcarded, which is not an accepted scenario (I.e -MachineName WebServer*, Database1)
                    if (LibrarySetName.Any(item => WildcardPattern.ContainsWildcardCharacters(item) && LibrarySetName.Count > 1))
                    {
                        throw OctoposhExceptions.ParameterCollectionHasRegularAndWildcardItem("LibrarySetName");
                    }
                    //Only 1 wildcarded value (ie -MachineName WebServer*)
                    else if (LibrarySetName.Any(item => WildcardPattern.ContainsWildcardCharacters(item) && LibrarySetName.Count == 1))
                    {
                        var pattern = new WildcardPattern(LibrarySetName.First());
                        libraryVariableSetList.AddRange(_connection.Repository.LibraryVariableSets.FindMany(t => pattern.IsMatch(t.Name.ToLower())));
                    }
                    //multiple non-wildcared values (i.e. -MachineName WebServer1,Database1)
                    else if (!LibrarySetName.Any(WildcardPattern.ContainsWildcardCharacters))
                    {
                        libraryVariableSetList.AddRange(_connection.Repository.LibraryVariableSets.FindMany(t => LibrarySetName.Contains(t.Name.ToLower())));
                    }
                    variableSetIDs.AddRange(libraryVariableSetList.Select(v => v.VariableSetId));
                }
                #endregion
            }

            //This works
            foreach (var id in variableSetIDs)
            {
                baseResourceList.Add(_connection.Repository.VariableSets.Get(id));
            }

            //This doesn't work and throws: [Exception thrown: 'Octopus.Client.Exceptions.OctopusResourceNotFoundException' in Octopus.Client.dll]
            //baseResourceList.AddRange(_connection.Repository.VariableSets.Get(variableSetIDs.ToArray()));

            ResourceOnly = true;

            if (ResourceOnly)
            {
                WriteObject(baseResourceList);
            }

            else
            {
                var converter = new OutputConverter();
                List<OutputOctopusVariableSet> outputList = converter.GetOctopusVariableSet(baseResourceList);

                WriteObject(outputList);
            }

        }
    }
}