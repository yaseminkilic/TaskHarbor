# scripts/pr.ps1
#
# Wraps the feature-branch → PR → squash-merge loop.
# Every git/gh command is printed in cyan before it runs, so the
# automation stays auditable: you see what it's doing at each step.
#
# Verbs:
#   start <short-name>      Sync main and create feature/<short-name>.
#   ship "<commit message>" Stage tracked changes + commit + push + open PR + watch CI.
#   land                    Squash-merge current PR, delete branch, sync main.
#   status                  Show branch + CI check status.
#
# Examples:
#   ./scripts/pr.ps1 start readme-badge
#   ./scripts/pr.ps1 ship "docs: add CI badge to README"
#   ./scripts/pr.ps1 land
#
# Note on staging: ship uses 'git add -u', which stages MODIFIED and DELETED
# tracked files. New (untracked) files are NOT auto-staged — add them
# explicitly with 'git add <path>' before running ship. This avoids the
# trap where untracked helper files (e.g. this script itself the first time
# around) get swept into your PR by mistake.

[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet('start', 'ship', 'land', 'status')]
    [string]$Verb,

    [Parameter(Position = 1)]
    [string]$Arg
)

$ErrorActionPreference = 'Stop'

function Run([string]$cmd) {
    Write-Host ""
    Write-Host "→ $cmd" -ForegroundColor Cyan
    Invoke-Expression $cmd
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        Write-Host "Command failed with exit $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

function CurrentBranch { git rev-parse --abbrev-ref HEAD }

function CurrentPr {
    try { gh pr view --json number -q .number 2>$null } catch { $null }
}

function Confirm([string]$prompt) {
    $reply = Read-Host "`n$prompt [y/N]"
    return ($reply -eq 'y' -or $reply -eq 'Y')
}

switch ($Verb) {

    'start' {
        if (-not $Arg) {
            throw "Usage: ./scripts/pr.ps1 start <short-name>   (creates feature/<short-name>)"
        }
        Run "git checkout main"
        Run "git pull --ff-only"
        Run "git checkout -b feature/$Arg"

        Write-Host ""
        Write-Host "Branch ready. Edit files, then:" -ForegroundColor Yellow
        Write-Host "  # for new files (untracked): git add <path>" -ForegroundColor Yellow
        Write-Host "  ./scripts/pr.ps1 ship `"<commit message>`"" -ForegroundColor Yellow
    }

    'ship' {
        if (-not $Arg) {
            throw "Usage: ./scripts/pr.ps1 ship `"<commit message>`""
        }
        $branch = CurrentBranch
        if ($branch -eq 'main') {
            throw "On main. Start a branch first: ./scripts/pr.ps1 start <name>"
        }

        Run "git status"

        if (-not (Confirm "Stage tracked changes + commit + push + open PR with title `"$Arg`"?")) {
            Write-Host "Aborted." -ForegroundColor Yellow
            return
        }

        # -u stages MODIFIED + DELETED tracked files only. New files must be
        # added explicitly before ship — see the note at the top of this file.
        Run "git add -u"
        Run "git commit -m `"$Arg`""
        Run "git push -u origin $branch"
        Run "gh pr create --title `"$Arg`" --body `"$Arg`""

        $pr = CurrentPr
        if ($pr) {
            Write-Host ""
            Write-Host "PR #$pr opened. Watching CI checks (Ctrl+C to detach)..." -ForegroundColor Yellow
            Run "gh pr checks $pr --watch"
        }
    }

    'land' {
        $pr = CurrentPr
        if (-not $pr) {
            throw "No PR found for the current branch. Run 'ship' first."
        }

        Run "gh pr view $pr"

        if (-not (Confirm "Squash-merge PR #$pr, delete branch, and return to main?")) {
            Write-Host "Aborted." -ForegroundColor Yellow
            return
        }

        Run "gh pr merge $pr --squash --delete-branch"
        Run "git checkout main"
        Run "git pull --ff-only"
        Run "git fetch --prune"
        Run "git log --oneline -5"
    }

    'status' {
        Run "git status"
        $pr = CurrentPr
        if ($pr) {
            Run "gh pr checks $pr"
        } else {
            Write-Host "`n(no PR for this branch)" -ForegroundColor Yellow
        }
    }
}
