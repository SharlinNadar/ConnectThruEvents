// Get token from local storage
function getToken() {
    return localStorage.getItem("token");
}

// Show login/logout button based on authentication status
function checkAuthentication() {
    const token = getToken();
    const logoutBtn = document.getElementById("logoutBtn");
    const loginBtn = document.querySelector(".login-btn"); // Select the login button using class

    if (token) {
        // User is authenticated, show logout button and hide login button
        if (logoutBtn) logoutBtn.style.display = "inline-block"; // Show logout button
        if (loginBtn) loginBtn.style.display = "none"; // Hide login button
    } else {
        // User is not authenticated, show login button and hide logout button
        if (logoutBtn) logoutBtn.style.display = "none"; // Hide logout button
        if (loginBtn) loginBtn.style.display = "inline-block"; // Show login button
    }
}

// Handle logout
function handleLogout() {
    // Remove the token from local storage and redirect to the homepage
    localStorage.removeItem("token");
    window.location.href = "/index.html"; // Redirect to the homepage (ensure the correct path)
}

// Initialize the home page
window.addEventListener("DOMContentLoaded", () => {
    checkAuthentication();

    // Logout button click handler
    const logoutBtn = document.getElementById("logoutBtn");
    if (logoutBtn) {
        logoutBtn.addEventListener("click", handleLogout);
    }
});

// register page redirect
function redirectToPage() {
    window.location.href = "reg.html"; // Replace with the path to your desired page
}
new Swiper('.card-wrapper', {
    loop: true,
    spaceBetween: 30,
  
    //  pagination bullets
    pagination: {
      el: '.swiper-pagination',
      clickable: true,
      dynamicBullets: true
    },
  
    // Navigation arrows
    navigation: {
      nextEl: '.swiper-button-next',
      prevEl: '.swiper-button-prev',
    },

    // responsive breakpoints
    breakpoints: {
        0: {
            slidesPerView: 1
        },
        768: {
            slidesPerView: 2
        },
        1024: {
            slidesPerView: 3
        },
    }
  });

  // Function to toggle the dropdown menu
  function toggleDropdown() {
    const dropdownMenu = document.getElementById('dropdownMenu');
    dropdownMenu.classList.toggle('show');
}

// Optional: Close the dropdown if the user clicks anywhere outside of it
window.onclick = function(event) {
    if (!event.target.matches('.dropdown-icon') && !event.target.matches('.dropdown-content')) {
        const dropdownMenu = document.getElementById('dropdownMenu');
        if (dropdownMenu.classList.contains('show')) {
            dropdownMenu.classList.remove('show');
        }
    }
}
// upcoming event
const countdownTimer = document.getElementById("countdown-timer");
const eventDate = new Date("January 10, 2025 00:00:00").getTime();

const interval = setInterval(function() {
    const now = new Date().getTime();
    const timeLeft = eventDate - now;

    const days = Math.floor(timeLeft / (1000 * 60 * 60 * 24));
    const hours = Math.floor((timeLeft % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutes = Math.floor((timeLeft % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((timeLeft % (1000 * 60)) / 1000);

    countdownTimer.innerHTML = `${days}d ${hours}h ${minutes}m ${seconds}s`;

    if (timeLeft < 0) {
        clearInterval(interval);
        countdownTimer.innerHTML = "The event has started!";
    }
}, 1000);
