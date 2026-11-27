document.getElementById('resultForm').addEventListener('submit', async function (e) {
    e.preventDefault();

    const studentData = {
        fullName: document.getElementById('fullName').value,
        matricNumber: document.getElementById('matricNumber').value,
        department: parseInt(document.getElementById('department').value),
        university: document.getElementById('university').value,
        firstScore: parseFloat(document.getElementById('firstScore').value),
        secondScore: parseFloat(document.getElementById('secondScore').value)
    };

    try {
        const response = await fetch('http://localhost:5138/api/calculate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(studentData)
        });

        const result = await response.json();

        if (!response.ok) {
            alert("Error: " + (result.error || "Invalid input"));
            return;
        }

        // Fill in the result card on the page
        document.getElementById('resName').textContent = result.fullName;
        document.getElementById('resMatric').textContent = result.matricNumber;
        document.getElementById('resDept').textContent = result.department;
        document.getElementById('resUni').textContent = result.university;
        document.getElementById('resTotal').textContent = result.totalScore;
        document.getElementById('resAverage').textContent = result.averageScore;
        document.getElementById('resGrade').textContent = result.grade;
        document.getElementById('resRemark').textContent = result.remark;

        // Style the PASS / FAIL badge
        const statusBadge = document.getElementById('resStatus');
        statusBadge.textContent = result.status;
        statusBadge.className = result.status === 'PASS' ? 'status-badge pass' : 'status-badge fail';

        // Reveal the result card
        document.getElementById('resultCard').style.display = 'block';

    } catch (error) {
        console.error("Connection error:", error);
        alert("Could not connect to the C# backend server. Make sure 'dotnet run' is running!");
    }
});