(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.layout').controller('Layout', Layout);

    // Layout.$inject = ['']

    function Layout() {
        var vm = this;
        vm.version = "test version";
    }
})(window, window.angular);
