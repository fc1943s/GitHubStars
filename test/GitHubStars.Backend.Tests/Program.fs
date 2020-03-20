namespace GitHubStars.Backend

open Expecto

module Program =

    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args IntegrationTests.tests

