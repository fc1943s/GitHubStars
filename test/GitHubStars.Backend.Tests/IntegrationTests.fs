namespace GitHubStars.Backend

open Expecto

module IntegrationTests =
    let tests = testList "Integration" [
        GitHubTests.tests
        DatabaseTests.tests
        ApiTests.tests
    ]

